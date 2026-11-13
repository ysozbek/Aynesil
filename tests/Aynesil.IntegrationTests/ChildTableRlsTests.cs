using FluentAssertions;
using Npgsql;

namespace Aynesil.IntegrationTests;

/// <summary>
/// V44 (Y2, Y3, Y4, Y6 root cause) and Y11 against PostgreSQL 17 with all migrations and the
/// afterMigrate grants, connected as aynesil_app:
///   - child tables without corporation_id follow their parent: another tenant's children are
///     invisible and not writable; children of global parents are readable but read-only;
///   - log partitions are not directly accessible (the partitioned parent still is);
///   - global catalogs are readable but read-only for the app.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class ChildTableRlsTests(MigratedDatabaseFixture db)
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

    private static async Task<PostgresException> RejectedAsync(Func<Task> act) =>
        (await act.Should().ThrowAsync<PostgresException>()).Which;

    private async Task<Guid> CorporationAsync(NpgsqlConnection owner, string tag) =>
        await ScalarAsync<Guid>(owner,
            "insert into core.corporation(code, legal_name, display_name) values (@c, @c, @c) returning id",
            ("c", $"{tag}_{_suffix}"));

    // ── Y2: iam.role_permission ───────────────────────────────────────────────

    [Fact]
    public async Task Role_permissions_follow_the_role()
    {
        await using var owner = await db.OpenAsync();
        var a = await CorporationAsync(owner, "rp_a");
        var b = await CorporationAsync(owner, "rp_b");
        var perm = await ScalarAsync<Guid>(owner, "select id from iam.permission where code = 'student:read'");
        var roleOf = async (Guid? corp, string code) => await ScalarAsync<Guid>(owner,
            "insert into iam.role(corporation_id, code, name) values (@c, @k, @k) returning id",
            ("c", (object?)corp ?? DBNull.Value), ("k", $"{code}_{_suffix}"));
        var own = await roleOf(a, "own");
        var foreign = await roleOf(b, "foreign");
        var global = await roleOf(null, "global");
        foreach (var r in new[] { own, foreign, global })
            await ExecAsync(owner, "insert into iam.role_permission(role_id, permission_id) values (@r, @p)", ("r", r), ("p", perm));

        await using var app = await db.OpenAsAppAsync(a);
        var visible = await ScalarAsync<long>(app,
            "select count(*) from iam.role_permission where role_id = any(@ids)", ("ids", new[] { own, foreign, global }));
        visible.Should().Be(2, "own + global role grants are readable, the other tenant's are not");

        (await ExecAsync(app, "delete from iam.role_permission where role_id = @r", ("r", foreign))).Should().Be(0);
        (await ExecAsync(app, "delete from iam.role_permission where role_id = @r", ("r", global))).Should().Be(0);
        (await RejectedAsync(() => ExecAsync(app,
            "insert into iam.role_permission(role_id, permission_id) values (@r, (select id from iam.permission where code = 'student:create'))",
            ("r", global)))).SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
        (await ExecAsync(app, "delete from iam.role_permission where role_id = @r", ("r", own))).Should().Be(1);

        (await ScalarAsync<long>(owner, "select count(*) from iam.role_permission where role_id = any(@ids)",
            ("ids", new[] { foreign, global }))).Should().Be(2);
    }

    // ── Y3: assessment_section / assessment_item ──────────────────────────────

    [Fact]
    public async Task Assessment_sections_and_items_follow_the_template()
    {
        await using var owner = await db.OpenAsync();
        var a = await CorporationAsync(owner, "as_a");
        var b = await CorporationAsync(owner, "as_b");
        var templateOf = async (Guid? corp, string code) => await ScalarAsync<Guid>(owner,
            "insert into assessment.assessment_template(corporation_id, code, name) values (@c, @k, @k) returning id",
            ("c", (object?)corp ?? DBNull.Value), ("k", $"{code}_{_suffix}"));
        var global = await templateOf(null, "g");
        var foreign = await templateOf(b, "f");
        var own = await templateOf(a, "o");
        var sectionOf = async (Guid template) => await ScalarAsync<Guid>(owner,
            "insert into assessment.assessment_section(template_id, code) values (@t, 's1') returning id", ("t", template));
        var globalSection = await sectionOf(global);
        var foreignSection = await sectionOf(foreign);
        await ExecAsync(owner,
            "insert into assessment.assessment_item(section_id, code, prompt, response_type) values (@s, 'i1', 'Soru', 'scale')",
            ("s", globalSection));

        await using var app = await db.OpenAsAppAsync(a);

        // Global template content is readable…
        (await ScalarAsync<long>(app, "select count(*) from assessment.assessment_section where id = @s", ("s", globalSection))).Should().Be(1);
        (await ScalarAsync<long>(app, "select count(*) from assessment.assessment_item where section_id = @s", ("s", globalSection))).Should().Be(1);
        // …but read-only.
        (await RejectedAsync(() => ExecAsync(app,
            "insert into assessment.assessment_section(template_id, code) values (@t, 'x')", ("t", global))))
            .SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
        (await ExecAsync(app, "update assessment.assessment_section set code = 'hijack' where id = @s", ("s", globalSection))).Should().Be(0);
        (await ExecAsync(app, "delete from assessment.assessment_item where section_id = @s", ("s", globalSection))).Should().Be(0);
        (await RejectedAsync(() => ExecAsync(app,
            "insert into assessment.assessment_item(section_id, code, prompt, response_type) values (@s, 'x', 'x', 'scale')",
            ("s", globalSection)))).SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);

        // Another tenant's template content is invisible and not writable.
        (await ScalarAsync<long>(app, "select count(*) from assessment.assessment_section where id = @s", ("s", foreignSection))).Should().Be(0);
        (await RejectedAsync(() => ExecAsync(app,
            "insert into assessment.assessment_section(template_id, code) values (@t, 'x')", ("t", foreign))))
            .SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);

        // Own template: full access.
        var ownSection = await ScalarAsync<Guid>(app,
            "insert into assessment.assessment_section(template_id, code) values (@t, 's1') returning id", ("t", own));
        (await ExecAsync(app,
            "insert into assessment.assessment_item(section_id, code, prompt, response_type) values (@s, 'i1', 'Soru', 'scale')",
            ("s", ownSection))).Should().Be(1);

        (await ScalarAsync<string>(owner, "select code from assessment.assessment_section where id = @s", ("s", globalSection))).Should().Be("s1");
    }

    // ── Y4: education.program_translation ─────────────────────────────────────

    [Fact]
    public async Task Program_translations_of_another_tenant_are_invisible_and_not_writable()
    {
        await using var owner = await db.OpenAsync();
        var a = await CorporationAsync(owner, "pt_a");
        var b = await CorporationAsync(owner, "pt_b");
        var foreign = await ScalarAsync<Guid>(owner,
            "insert into education.program(corporation_id, code, name) values (@c, 'p', 'B') returning id", ("c", b));
        await ExecAsync(owner, "insert into education.program_translation(program_id, locale, name) values (@p, 'en', 'Original')", ("p", foreign));

        await using var app = await db.OpenAsAppAsync(a);
        (await ScalarAsync<long>(app, "select count(*) from education.program_translation where program_id = @p", ("p", foreign))).Should().Be(0);
        (await ExecAsync(app, "update education.program_translation set name = 'Hijacked' where program_id = @p", ("p", foreign))).Should().Be(0);
        (await RejectedAsync(() => ExecAsync(app,
            "insert into education.program_translation(program_id, locale, name) values (@p, 'tr', 'Hijacked')", ("p", foreign))))
            .SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);

        (await ScalarAsync<string>(owner, "select name from education.program_translation where program_id = @p", ("p", foreign))).Should().Be("Original");
    }

    // ── Platform menu labels are shared ───────────────────────────────────────

    [Fact]
    public async Task Platform_menu_translations_are_readable_but_not_writable()
    {
        await using var owner = await db.OpenAsync();
        var a = await CorporationAsync(owner, "mt_a");
        var item = await ScalarAsync<Guid>(owner, "select id from iam.menu_item where corporation_id is null limit 1");

        await using var app = await db.OpenAsAppAsync(a);
        (await ScalarAsync<long>(app, "select count(*) from iam.menu_item_translation where menu_item_id = @m", ("m", item))).Should().BeGreaterThan(0);
        (await ExecAsync(app, "update iam.menu_item_translation set label = 'x' where menu_item_id = @m", ("m", item))).Should().Be(0);
        (await ExecAsync(app, "delete from iam.menu_item_translation where menu_item_id = @m", ("m", item))).Should().Be(0);
    }

    // ── Y6: ops.survey_question_response ──────────────────────────────────────

    [Fact]
    public async Task Survey_answer_cannot_reference_another_tenants_question()
    {
        await using var owner = await db.OpenAsync();
        var a = await CorporationAsync(owner, "sq_a");
        var b = await CorporationAsync(owner, "sq_b");
        var surveyOf = async (Guid corp) => await ScalarAsync<Guid>(owner,
            "insert into ops.survey_definition(corporation_id, title) values (@c, 'Anket') returning id", ("c", corp));
        var questionOf = async (Guid survey) => await ScalarAsync<Guid>(owner,
            "insert into ops.survey_question(survey_id, question_text) values (@s, 'Soru?') returning id", ("s", survey));
        var ownSurvey = await surveyOf(a);
        var ownQuestion = await questionOf(ownSurvey);
        var foreignQuestion = await questionOf(await surveyOf(b));

        await using var app = await db.OpenAsAppAsync(a);
        var response = await ScalarAsync<Guid>(app,
            "insert into ops.survey_response(corporation_id, survey_id) values (@c, @s) returning id", ("c", a), ("s", ownSurvey));

        (await ScalarAsync<long>(app, "select count(*) from ops.survey_question where id = @q", ("q", foreignQuestion))).Should().Be(0);
        (await RejectedAsync(() => ExecAsync(app,
            "insert into ops.survey_question_response(response_id, question_id, answer_text) values (@r, @q, 'x')",
            ("r", response), ("q", foreignQuestion)))).SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
        (await ExecAsync(app,
            "insert into ops.survey_question_response(response_id, question_id, answer_text) values (@r, @q, 'Evet')",
            ("r", response), ("q", ownQuestion))).Should().Be(1);
    }

    // ── Y11 + global catalogs ─────────────────────────────────────────────────

    [Theory]
    [InlineData("core.audit_log_default", "core.audit_log")]
    [InlineData("core.activity_log_default", "core.activity_log")]
    [InlineData("core.integration_log_default", "core.integration_log")]
    [InlineData("media.viewing_log_default", "media.viewing_log")]
    public async Task Log_partitions_are_not_directly_accessible_but_the_parent_is(string partition, string parent)
    {
        await using var owner = await db.OpenAsync();
        var a = await CorporationAsync(owner, "lp");
        await using var app = await db.OpenAsAppAsync(a);

        (await RejectedAsync(() => ScalarAsync<long>(app, $"select count(*) from {partition}")))
            .SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
        var parentRead = () => ScalarAsync<long>(app, $"select count(*) from {parent}");
        await parentRead.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("iam.permission", "insert into iam.permission(code, resource, action) values ('x:y', 'x', 'y')")]
    [InlineData("ref.ref_type", "update ref.ref_type set name = name")]
    [InlineData("ref.locale", "delete from ref.locale where code = 'zz'")]
    [InlineData("ref.i18n_message", "update ref.i18n_message set value = value")]
    [InlineData("core.setting_definition", "update core.setting_definition set key = key")]
    [InlineData("core.integration_provider", "delete from core.integration_provider where code = 'zz'")]
    public async Task Global_catalogs_are_read_only_for_the_app(string table, string write)
    {
        await using var owner = await db.OpenAsync();
        var a = await CorporationAsync(owner, "gc");
        await using var app = await db.OpenAsAppAsync(a);

        var read = () => ScalarAsync<long>(app, $"select count(*) from {table}");
        await read.Should().NotThrowAsync();
        (await RejectedAsync(() => ExecAsync(app, write))).SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
    }
}
