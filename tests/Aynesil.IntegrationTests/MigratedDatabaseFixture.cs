using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Aynesil.IntegrationTests;

/// <summary>
/// PostgreSQL 17 with every Flyway migration applied by the real Flyway image, using the repo's
/// docker/init-db.sh (app role) and docker/flyway.conf — the same setup as docker-compose.
/// Shared by all integration tests (one container per test run).
/// </summary>
public sealed class MigratedDatabaseFixture : IAsyncLifetime
{
    private const string Database = "aynesil";
    private const string Owner = "aynesil_owner";
    private const string OwnerPassword = "owner_it";

    private readonly INetwork _network = new NetworkBuilder().Build();
    private PostgreSqlContainer _postgres = null!;

    /// <summary>
    /// Owner connection (bypasses RLS like Flyway/seed). Deliberately without
    /// "Include Error Detail" so error classification is tested with the production default,
    /// where Npgsql redacts PostgresException.Detail.
    /// </summary>
    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var root = RepoRoot();
        await _network.CreateAsync();

        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .WithNetwork(_network)
            .WithNetworkAliases("pg")
            .WithDatabase(Database)
            .WithUsername(Owner)
            .WithPassword(OwnerPassword)
            .WithEnvironment("AYNESIL_DB_APP_USER", "aynesil_app")
            .WithEnvironment("AYNESIL_DB_APP_PASSWORD", "app_it")
            .WithBindMount(Path.Combine(root, "docker", "init-db.sh"),
                "/docker-entrypoint-initdb.d/01-init-db.sh", AccessMode.ReadOnly)
            .Build();
        await _postgres.StartAsync();

        var flyway = new ContainerBuilder()
            .WithImage("flyway/flyway:10.20-alpine")
            .WithNetwork(_network)
            .WithBindMount(Path.Combine(root, "db", "migrations"), "/flyway/sql", AccessMode.ReadOnly)
            .WithBindMount(Path.Combine(root, "docker", "flyway.conf"), "/flyway/conf/flyway.conf", AccessMode.ReadOnly)
            .WithEnvironment("FLYWAY_URL", $"jdbc:postgresql://pg:5432/{Database}")
            .WithEnvironment("FLYWAY_USER", Owner)
            .WithEnvironment("FLYWAY_PASSWORD", OwnerPassword)
            .WithCommand("migrate")
            .Build();
        await flyway.StartAsync();
        var exitCode = await flyway.GetExitCodeAsync();
        if (exitCode != 0)
        {
            var (stdout, stderr) = await flyway.GetLogsAsync();
            throw new InvalidOperationException($"Flyway migrate failed (exit {exitCode}):\n{stdout}\n{stderr}");
        }
        await flyway.DisposeAsync();

        ConnectionString = _postgres.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _network.DisposeAsync();
    }

    /// <summary>
    /// Connection as the least-privilege application role (subject to RLS), scoped to a tenant
    /// the same way TenantConnectionInterceptor does (app.current_corporation_id).
    /// </summary>
    public async Task<NpgsqlConnection> OpenAsAppAsync(Guid corporationId)
    {
        var builder = new NpgsqlConnectionStringBuilder(ConnectionString) { Username = "aynesil_app", Password = "app_it" };
        var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand("select set_config('app.current_corporation_id', @c, false)", conn);
        cmd.Parameters.AddWithValue("c", corporationId.ToString());
        await cmd.ExecuteNonQueryAsync();
        return conn;
    }

    public async Task<NpgsqlConnection> OpenAsync()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    private static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "Aynesil.sln")))
                return dir.FullName;
        throw new InvalidOperationException("Repository root (Aynesil.sln) not found.");
    }
}

[CollectionDefinition(Name)]
public sealed class MigratedDatabaseCollection : ICollectionFixture<MigratedDatabaseFixture>
{
    public const string Name = "Migrated PostgreSQL 17";
}
