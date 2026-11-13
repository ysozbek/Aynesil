using FluentAssertions;
using Npgsql;

namespace Aynesil.IntegrationTests;

/// <summary>
/// V45: tenant_isolation is split per command. A tenant can read global rows
/// (corporation_id IS NULL) but cannot delete them — DELETE affects 0 rows — nor re-own them via UPDATE.
/// Handlers translate the same rule into 403 (Rule.PlatformRecordReadOnly) before reaching the DB.
/// Connected as aynesil_app with all migrations applied.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class GlobalRowDeleteTests(MigratedDatabaseFixture db)
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

    private async Task<Guid> CorporationAsync(NpgsqlConnection owner, string tag) =>
        await ScalarAsync<Guid>(owner,
            "insert into core.corporation(code, legal_name, display_name) values (@c, @c, @c) returning id",
            ("c", $"{tag}_{_suffix}"));

    [Fact]
    public async Task Tenant_cannot_delete_a_global_ref_value_but_can_delete_its_own()
    {
        await using var owner = await db.OpenAsync();
        var a = await CorporationAsync(owner, "gd_ref");
        var typeId = await ScalarAsync<Guid>(owner, "select id from ref.ref_type limit 1");
        var insert = "insert into ref.ref_value(ref_type_id, corporation_id, code) values (@t, @c, @k) returning id";
        var global = await ScalarAsync<Guid>(owner, insert, ("t", typeId), ("c", DBNull.Value), ("k", $"g_{_suffix}"));
        var own = await ScalarAsync<Guid>(owner, insert, ("t", typeId), ("c", a), ("k", $"o_{_suffix}"));

        await using var app = await db.OpenAsAppAsync(a);
        (await ScalarAsync<long>(app, "select count(*) from ref.ref_value where id = @i", ("i", global))).Should().Be(1, "global rows stay readable");

        (await ExecAsync(app, "delete from ref.ref_value where id = @i", ("i", global))).Should().Be(0);
        (await ExecAsync(app, "delete from ref.ref_value where id = @i", ("i", own))).Should().Be(1);
        (await ScalarAsync<long>(owner, "select count(*) from ref.ref_value where id = @i", ("i", global))).Should().Be(1);
    }

    [Fact]
    public async Task Tenant_cannot_delete_a_global_template_but_can_delete_its_own()
    {
        await using var owner = await db.OpenAsync();
        var a = await CorporationAsync(owner, "gd_tpl");
        var insert = "insert into assessment.assessment_template(corporation_id, code, name) values (@c, @k, @k) returning id";
        var global = await ScalarAsync<Guid>(owner, insert, ("c", DBNull.Value), ("k", $"g_{_suffix}"));
        var own = await ScalarAsync<Guid>(owner, insert, ("c", a), ("k", $"o_{_suffix}"));

        await using var app = await db.OpenAsAppAsync(a);
        (await ExecAsync(app, "delete from assessment.assessment_template where id = @i", ("i", global))).Should().Be(0);
        (await ExecAsync(app, "delete from assessment.assessment_template where id = @i", ("i", own))).Should().Be(1);
        (await ScalarAsync<long>(owner, "select count(*) from assessment.assessment_template where id = @i", ("i", global))).Should().Be(1);
    }

    [Fact]
    public async Task Tenant_cannot_update_or_adopt_a_global_row()
    {
        await using var owner = await db.OpenAsync();
        var a = await CorporationAsync(owner, "gd_upd");
        var global = await ScalarAsync<Guid>(owner,
            "insert into assessment.assessment_template(corporation_id, code, name) values (null, @k, @k) returning id",
            ("k", $"g_{_suffix}"));

        await using var app = await db.OpenAsAppAsync(a);
        // Targetable by USING, but WITH CHECK demands the tenant's own corporation_id: the change is rejected.
        var rejected = await FluentActions.Awaiting(() => ExecAsync(app,
            "update assessment.assessment_template set name = 'hijack' where id = @i", ("i", global)))
            .Should().ThrowAsync<PostgresException>();
        rejected.Which.SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
    }

    [Fact]
    public async Task Global_menu_items_are_read_only_for_tenants()
    {
        await using var owner = await db.OpenAsync();
        var a = await CorporationAsync(owner, "gd_menu");
        var global = await ScalarAsync<Guid>(owner, "select id from iam.menu_item where corporation_id is null limit 1");

        await using var app = await db.OpenAsAppAsync(a);
        (await ScalarAsync<long>(app, "select count(*) from iam.menu_item where id = @i", ("i", global))).Should().Be(1);
        (await ExecAsync(app, "update iam.menu_item set sort_order = sort_order + 1, is_active = false where id = @i", ("i", global)))
            .Should().Be(0, "menu_item_update no longer targets platform rows");
        (await ExecAsync(app, "delete from iam.menu_item where id = @i", ("i", global))).Should().Be(0);
    }
}
