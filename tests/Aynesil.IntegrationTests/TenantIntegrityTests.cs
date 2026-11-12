using Aynesil.Application.Common.Localization;
using System.Text;
using Aynesil.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Aynesil.IntegrationTests;

/// <summary>
/// #86 against a real PostgreSQL 17 with all Flyway migrations (V38–V41 included):
/// composite tenant FKs, the global-or-tenant reference trigger, ON DELETE preservation and
/// the API's 23503 → 422 / 409 mapping. Each test creates its own corporations and rows.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class TenantIntegrityTests(MigratedDatabaseFixture db)
{
    // ── Composite tenant FK ───────────────────────────────────────────────────

    [Fact]
    public async Task Insert_referencing_a_record_of_another_tenant_is_rejected()
    {
        await using var conn = await db.OpenAsync();
        var a = await NewCorporationAsync(conn);
        var b = await NewCorporationAsync(conn);
        var foreignStudent = await NewStudentAsync(conn, b);

        var ex = await CaptureAsync(() => ExecAsync(conn,
            "insert into scheduling.makeup_request (corporation_id, student_id) values (@c, @s)",
            ("c", a), ("s", foreignStudent)));

        ex.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        ex.ConstraintName.Should().Be("fk_makeup_request_student_id_tnt");
        (await MapAsync(ex)).Should().Be((422, MsgKeys.Error.ReferenceMissing.Value));
    }

    [Fact]
    public async Task Insert_referencing_a_record_of_the_same_tenant_is_accepted()
    {
        await using var conn = await db.OpenAsync();
        var a = await NewCorporationAsync(conn);
        var student = await NewStudentAsync(conn, a);

        var act = () => ExecAsync(conn,
            "insert into scheduling.makeup_request (corporation_id, student_id) values (@c, @s)",
            ("c", a), ("s", student));

        await act.Should().NotThrowAsync();
    }

    // ── Global-or-tenant reference trigger (ref.ref_value) ────────────────────

    [Fact]
    public async Task Global_ref_value_is_accepted()
    {
        await using var conn = await db.OpenAsync();
        var a = await NewCorporationAsync(conn);
        var globalStatus = await ScalarAsync<Guid>(conn, """
            select v.id from ref.ref_value v join ref.ref_type t on t.id = v.ref_type_id
             where t.code = 'student_status' and v.corporation_id is null limit 1
            """);

        var act = () => NewStudentAsync(conn, a, globalStatus);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Ref_value_of_the_same_tenant_is_accepted()
    {
        await using var conn = await db.OpenAsync();
        var a = await NewCorporationAsync(conn);
        var ownStatus = await NewTenantRefValueAsync(conn, a, "student_status");

        var act = () => NewStudentAsync(conn, a, ownStatus);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Ref_value_of_another_tenant_is_rejected()
    {
        await using var conn = await db.OpenAsync();
        var a = await NewCorporationAsync(conn);
        var b = await NewCorporationAsync(conn);
        var foreignStatus = await NewTenantRefValueAsync(conn, b, "student_status");

        var ex = await CaptureAsync(() => NewStudentAsync(conn, a, foreignStatus));

        ex.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        ex.ColumnName.Should().Be("status_id");
        (await MapAsync(ex)).Should().Be((422, MsgKeys.Error.ReferenceMissing.Value));
    }

    // ── Delete of a record that is still in use ───────────────────────────────

    [Fact]
    public async Task Deleting_a_record_in_use_maps_to_409()
    {
        await using var conn = await db.OpenAsync();
        var a = await NewCorporationAsync(conn);
        var student = await NewStudentAsync(conn, a);
        await ExecAsync(conn,
            "insert into scheduling.makeup_request (corporation_id, student_id) values (@c, @s)",
            ("c", a), ("s", student));

        var ex = await CaptureAsync(() => ExecAsync(conn,
            "delete from students.student where id = @s", ("s", student)));

        ex.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        (await MapAsync(ex)).Should().Be((409, MsgKeys.Error.ReferenceInUse.Value));
        // Same mapping when the error arrives wrapped by EF Core SaveChanges.
        (await MapAsync(new DbUpdateException("wrapped", ex))).Item1.Should().Be(409);
    }

    // ── ON DELETE behaviour preserved ─────────────────────────────────────────

    [Fact]
    public async Task On_delete_set_null_clears_only_the_reference_column()
    {
        await using var conn = await db.OpenAsync();
        var a = await NewCorporationAsync(conn);
        var session = await NewSessionAsync(conn, a);
        var campId = await ScalarAsync<Guid>(conn,
            "insert into camps.camp (corporation_id, code, name) values (@c, @code, 'Yaz') returning id",
            ("c", a), ("code", Code()));
        var periodId = await ScalarAsync<Guid>(conn, """
            insert into camps.camp_period (corporation_id, camp_id, name, start_date, end_date)
            values (@c, @camp, 'Dönem', date '2026-07-01', date '2026-07-14') returning id
            """, ("c", a), ("camp", campId));
        var activityId = await ScalarAsync<Guid>(conn, """
            insert into camps.camp_activity (corporation_id, camp_period_id, name, session_id)
            values (@c, @p, 'Yüzme', @s) returning id
            """, ("c", a), ("p", periodId), ("s", session));

        await ExecAsync(conn, "delete from scheduling.session where id = @s", ("s", session));

        var (sessionId, corporationId) = await RowAsync(conn,
            "select session_id, corporation_id from camps.camp_activity where id = @id", ("id", activityId));
        sessionId.Should().BeNull("ON DELETE SET NULL (session_id) must clear the reference");
        corporationId.Should().Be(a, "the NOT NULL corporation_id must stay intact");
    }

    [Fact]
    public async Task On_delete_cascade_is_preserved()
    {
        await using var conn = await db.OpenAsync();
        var a = await NewCorporationAsync(conn);
        var student = await NewStudentAsync(conn, a);
        await ExecAsync(conn, "insert into education.enrollment (corporation_id, student_id) values (@c, @s)",
            ("c", a), ("s", student));

        await ExecAsync(conn, "delete from students.student where id = @s", ("s", student));

        (await ScalarAsync<long>(conn, "select count(*) from education.enrollment where student_id = @s", ("s", student)))
            .Should().Be(0);
    }

    // ── V42 ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Consultancy_agreements_and_follow_ups_are_isolated_by_rls()   // #87
    {
        await using var owner = await db.OpenAsync();
        var a = await NewCorporationAsync(owner);
        var b = await NewCorporationAsync(owner);
        var institution = await ScalarAsync<Guid>(owner,
            "insert into consultancy.institution (corporation_id, name) values (@c, 'Okul') returning id", ("c", b));
        var plan = await ScalarAsync<Guid>(owner,
            "insert into consultancy.consultancy_plan (corporation_id, institution_id, name) values (@c, @i, 'Plan') returning id",
            ("c", b), ("i", institution));
        await ExecAsync(owner,
            "insert into consultancy.consultancy_agreement (corporation_id, consultancy_plan_id, institution_id, title) values (@c, @p, @i, 'Sözleşme')",
            ("c", b), ("p", plan), ("i", institution));
        await ExecAsync(owner,
            "insert into consultancy.follow_up_activity (corporation_id, consultancy_plan_id, title) values (@c, @p, 'Takip')",
            ("c", b), ("p", plan));

        await using var appAsA = await db.OpenAsAppAsync(a);
        (await ScalarAsync<long>(appAsA, "select count(*) from consultancy.consultancy_agreement where corporation_id = @c", ("c", b)))
            .Should().Be(0, "tenant A must not see tenant B's agreements");
        (await ScalarAsync<long>(appAsA, "select count(*) from consultancy.follow_up_activity where corporation_id = @c", ("c", b)))
            .Should().Be(0, "tenant A must not see tenant B's follow-ups");

        await using var appAsB = await db.OpenAsAppAsync(b);
        (await ScalarAsync<long>(appAsB, "select count(*) from consultancy.consultancy_agreement where corporation_id = @c", ("c", b)))
            .Should().Be(1, "the owning tenant still sees its own rows");
    }

    [Fact]
    public async Task Viewing_log_ip_address_accepts_an_inet_value()
    {
        await using var conn = await db.OpenAsync();
        var a = await NewCorporationAsync(conn);

        // The EF model maps ViewingLog.IpAddress as System.Net.IPAddress -> inet.
        var stored = await ScalarAsync<string>(conn,
            "insert into media.viewing_log (corporation_id, ip_address) values (@c, @ip) returning host(ip_address)",
            ("c", a), ("ip", System.Net.IPAddress.Parse("10.1.2.3")));

        stored.Should().Be("10.1.2.3");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Code() => "it" + Guid.NewGuid().ToString("N")[..10];

    private static Task<Guid> NewCorporationAsync(NpgsqlConnection conn) =>
        ScalarAsync<Guid>(conn,
            "insert into core.corporation (code, legal_name, display_name) values (@code, 'IT Kurumu', 'IT') returning id",
            ("code", Code()));

    private static Task<Guid> NewStudentAsync(NpgsqlConnection conn, Guid corporationId, Guid? statusId = null) =>
        ScalarAsync<Guid>(conn, """
            insert into students.student (corporation_id, first_name, last_name, status_id)
            values (@c, 'Ali', 'Kaya', @st) returning id
            """, ("c", corporationId), ("st", (object?)statusId ?? DBNull.Value));

    private static Task<Guid> NewTenantRefValueAsync(NpgsqlConnection conn, Guid corporationId, string refType) =>
        ScalarAsync<Guid>(conn, """
            insert into ref.ref_value (ref_type_id, corporation_id, code)
            values ((select id from ref.ref_type where code = @t), @c, @code) returning id
            """, ("t", refType), ("c", corporationId), ("code", Code()));

    private static Task<Guid> NewSessionAsync(NpgsqlConnection conn, Guid corporationId) =>
        ScalarAsync<Guid>(conn, """
            insert into scheduling.session (corporation_id, session_type_id, starts_at, ends_at)
            values (@c,
                    (select v.id from ref.ref_value v join ref.ref_type t on t.id = v.ref_type_id
                      where t.code = 'session_type' and v.corporation_id is null limit 1),
                    now() + interval '1 day', now() + interval '1 day 40 minutes')
            returning id
            """, ("c", corporationId));

    private static NpgsqlCommand Command(NpgsqlConnection conn, string sql, (string Name, object Value)[] args)
    {
        var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in args) cmd.Parameters.AddWithValue(name, value);
        return cmd;
    }

    private static async Task ExecAsync(NpgsqlConnection conn, string sql, params (string, object)[] args)
    {
        await using var cmd = Command(conn, sql, args);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection conn, string sql, params (string, object)[] args)
    {
        await using var cmd = Command(conn, sql, args);
        return (T)(await cmd.ExecuteScalarAsync())!;
    }

    private static async Task<(Guid? First, Guid Second)> RowAsync(NpgsqlConnection conn, string sql, params (string, object)[] args)
    {
        await using var cmd = Command(conn, sql, args);
        await using var reader = await cmd.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        return (reader.IsDBNull(0) ? null : reader.GetGuid(0), reader.GetGuid(1));
    }

    private static async Task<PostgresException> CaptureAsync(Func<Task> act)
    {
        var ex = await Record.ExceptionAsync(act);
        return ex.Should().BeOfType<PostgresException>().Subject;
    }

    /// <summary>Runs the exception through the real ExceptionMiddleware; returns (status, error code). The text is locale-dependent, so the stable code is asserted.</summary>
    private static async Task<(int, string)> MapAsync(Exception ex)
    {
        var middleware = new ExceptionMiddleware(_ => throw ex,
            NullLogger<ExceptionMiddleware>.Instance);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = Encoding.UTF8.GetString(((MemoryStream)context.Response.Body).ToArray());
        var code = System.Text.Json.JsonDocument.Parse(body).RootElement.GetProperty("code").GetString()!;
        return (context.Response.StatusCode, code);
    }
}
