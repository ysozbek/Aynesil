using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Aynesil.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core CLI for migrations and scaffolding.
/// Connects using the OWNER role (bypasses RLS) so migrations can modify schema.
/// The application runtime MUST connect using the non-owner 'aynesil_app' role
/// so that PostgreSQL RLS tenant isolation is active.
/// </summary>
public class AynesilDbContextFactory : IDesignTimeDbContextFactory<AynesilDbContext>
{
    public AynesilDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables("AYNESIL_")
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=aynesil;Username=aynesil_owner;Password=changeme";

        var optionsBuilder = new DbContextOptionsBuilder<AynesilDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsHistoryTable("__ef_migrations_history", "core");
            npgsql.EnableRetryOnFailure(maxRetryCount: 3);
        });

        return new AynesilDbContext(optionsBuilder.Options);
    }
}
