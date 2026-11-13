using FluentAssertions;
using Npgsql;

namespace Aynesil.IntegrationTests;

/// <summary>V46 (Y10): DB, iptal edilmiş faturada gerekçe olmasına izin vermez.</summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class InvoiceVoidReasonTests(MigratedDatabaseFixture db)
{
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection conn, string sql, params (string, object)[] args)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (k, v) in args) cmd.Parameters.AddWithValue(k, v);
        return (T)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task<int> ExecAsync(NpgsqlConnection conn, string sql, params (string, object)[] args)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (k, v) in args) cmd.Parameters.AddWithValue(k, v);
        return await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task A_void_invoice_requires_a_reason()
    {
        await using var owner = await db.OpenAsync();
        var corp = await ScalarAsync<Guid>(owner,
            "insert into core.corporation(code, legal_name, display_name) values (@c, @c, @c) returning id", ("c", $"iv_{_suffix}"));
        var invoice = await ScalarAsync<Guid>(owner,
            "insert into finance.invoice(corporation_id) values (@c) returning id", ("c", corp));

        foreach (var blank in new object[] { DBNull.Value, "", "   " })
        {
            var ex = await FluentActions.Awaiting(() => ExecAsync(owner,
                "update finance.invoice set status = 'void', void_reason = @r where id = @i", ("r", blank), ("i", invoice)))
                .Should().ThrowAsync<PostgresException>();
            ex.Which.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        }

        (await ExecAsync(owner, "update finance.invoice set status = 'void', void_reason = 'Hatalı fatura' where id = @i", ("i", invoice)))
            .Should().Be(1);
    }
}
