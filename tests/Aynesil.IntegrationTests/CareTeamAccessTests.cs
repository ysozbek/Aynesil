using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace Aynesil.IntegrationTests;

/// <summary>
/// Bakım ekibi uçları (6) ve öğrenci bazlı erişim kısıtı gerçek HTTP hattında (aynesil_app, RLS etkin).
/// Kısıt, kurumun <c>care_team_abac_enabled</c> ayarını açmasıyla devreye girer (platform/sahip yazar; arayüzü yok,
/// bkz. KURULUM_KILAVUZU). Açıkken bakım ekibinde olmayan, care_team:bypass'ı bulunmayan eğitmen o öğrencinin klinik
/// kayıtlarını (hedef, vaka notu, BEP …) göremez; ekibe eklenince görür, ekipten çıkarılınca tekrar kaybeder.
/// Not: öğrenci kartlarının listesi (students.student) bu kısıta bağlı değildir; kısıt klinik tablolardadır.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class CareTeamAccessTests : IDisposable
{
    private const string UserPassword = "Egitmen#Sifre1";
    private readonly TenantApiHarness _h;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public CareTeamAccessTests(MigratedDatabaseFixture db) => _h = new TenantApiHarness(db);

    public void Dispose() => _h.Dispose();

    private static int Total(JsonElement body) => body.GetProperty("data").GetProperty("totalCount").GetInt32();

    private async Task<(Guid Corp, string AdminToken, string EducatorToken, Guid EducatorId, Guid StudentA, Guid StudentB)> ArrangeAsync()
    {
        var admin = $"admin-cta-{_suffix}@example.test";
        var corp = await _h.NewTenantAsync($"cta_{_suffix}", admin);
        var adminToken = TenantApiHarness.Token(await _h.LoginAsync(admin, TenantApiHarness.AdminPassword));

        // Bakım ekibi modülü (new_tenant.py ile kapalı başlar) + öğrenci bazlı kısıt açık.
        await _h.ExecAsync(
            "update core.corporation set settings = jsonb_set(settings, '{modules,care_team}', 'true'::jsonb, true) where id = @c", ("c", corp));
        await _h.ExecAsync(
            "insert into core.setting_value(setting_key, scope_level, corporation_id, value) values ('care_team_abac_enabled', 'corporation', @c, 'true')",
            ("c", corp));

        var studentA = Guid.NewGuid();
        var studentB = Guid.NewGuid();
        foreach (var (id, name) in new[] { (studentA, "Ada"), (studentB, "Bora") })
        {
            await _h.ExecAsync("insert into students.student(id, corporation_id, first_name, last_name) values (@id, @c, @n, 'Test')",
                ("id", id), ("c", corp), ("n", name));
            await _h.ExecAsync("insert into education.student_goal(corporation_id, student_id, statement) values (@c, @s, 'Göz teması')",
                ("c", corp), ("s", id));
        }

        // Eğitmen rolü: yalnızca hedef okuma + bakım ekibi okuma (care_team:bypass yok).
        var (rs, role) = await _h.SendAsync(HttpMethod.Post, "/api/roles", adminToken,
            new { code = $"egitmen_{_suffix}", name = "Eğitmen", description = "test" });
        rs.Should().Be(HttpStatusCode.Created, role.ToString());
        var roleId = role.GetProperty("data").GetProperty("id").GetGuid();
        foreach (var code in new[] { "student_goal:read", "care_team:read" })
        {
            var permissionId = await _h.ScalarAsync<Guid>("select id from iam.permission where code = @c", ("c", code));
            (await _h.SendAsync(HttpMethod.Post, $"/api/roles/{roleId}/permissions", adminToken, new { permissionId }))
                .Status.Should().Be(HttpStatusCode.Created);
        }
        var username = $"egitmen-cta-{_suffix}@example.test";
        var userId = await _h.CreateUserAsync(adminToken, username, UserPassword);
        (await _h.SendAsync(HttpMethod.Post, $"/api/users/{userId}/roles", adminToken, new { roleId, campusId = (Guid?)null }))
            .Status.Should().Be(HttpStatusCode.Created);
        var educatorId = Guid.NewGuid();
        await _h.ExecAsync("insert into educators.educator(id, corporation_id, user_id, first_name, last_name) values (@id, @c, @u, 'Ayşe', 'Eğitmen')",
            ("id", educatorId), ("c", corp), ("u", userId));
        var token = TenantApiHarness.Token(await _h.LoginAsync(username, UserPassword));
        return (corp, adminToken, token, educatorId, studentA, studentB);
    }

    [Fact]
    public async Task With_the_restriction_on_an_educator_sees_only_students_in_their_care_team()
    {
        var (_, admin, educator, educatorId, studentA, studentB) = await ArrangeAsync();
        var roleId = await _h.ScalarAsync<Guid>(
            "select v.id from ref.ref_value v join ref.ref_type t on t.id = v.ref_type_id where t.code = 'care_team_role' and v.code = 'primary_therapist' limit 1");
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        // Ekip boş: kısıt açık, eğitmen hiçbir öğrencinin klinik kaydını göremez; "benim öğrencilerim" boş.
        (await _h.SendAsync(HttpMethod.Get, $"/api/goals/student-goals?studentId={studentA}", educator)).Body.Pipe(Total).Should().Be(0);
        var (s0, mine0) = await _h.SendAsync(HttpMethod.Get, "/api/care-team/my-students", educator);
        s0.Should().Be(HttpStatusCode.OK, mine0.ToString());
        mine0.GetProperty("data").GetArrayLength().Should().Be(0);

        // Yönetici eğitmeni A öğrencisinin ekibine ekler.
        var (sa, assigned) = await _h.SendAsync(HttpMethod.Post, $"/api/students/{studentA}/care-team", admin,
            new { educatorId, roleId, isPrimary = true, activeFrom = today });
        sa.Should().Be(HttpStatusCode.Created, assigned.ToString());
        var assignmentId = assigned.GetProperty("data").GetProperty("id").GetGuid();

        // Eğitmen yalnızca A'yı görür, B'yi görmez.
        (await _h.SendAsync(HttpMethod.Get, $"/api/goals/student-goals?studentId={studentA}", educator)).Body.Pipe(Total).Should().Be(1);
        (await _h.SendAsync(HttpMethod.Get, $"/api/goals/student-goals?studentId={studentB}", educator)).Body.Pipe(Total).Should().Be(0);
        var (_, mine) = await _h.SendAsync(HttpMethod.Get, "/api/care-team/my-students", educator);
        var students = mine.GetProperty("data").EnumerateArray().ToList();
        students.Should().ContainSingle();
        students[0].GetProperty("studentId").GetGuid().Should().Be(studentA);
        students[0].GetProperty("isPrimary").GetBoolean().Should().BeTrue();

        // Liste, güncelleme ve kaldırma için gereken rowVersion'ı döndürür.
        var (_, list) = await _h.SendAsync(HttpMethod.Get, $"/api/students/{studentA}/care-team", admin);
        var item = list.GetProperty("data").EnumerateArray().Single();
        item.GetProperty("educatorName").GetString().Should().Be("Ayşe Eğitmen");
        var rowVersion = item.GetProperty("rowVersion").GetInt32();
        rowVersion.Should().BeGreaterThan(0);

        var (su, updated) = await _h.SendAsync(HttpMethod.Put, $"/api/students/{studentA}/care-team/{assignmentId}", admin,
            new { isPrimary = false, reason = "rol değişti", rowVersion });
        su.Should().Be(HttpStatusCode.OK, updated.ToString());
        var (_, one) = await _h.SendAsync(HttpMethod.Get, $"/api/students/{studentA}/care-team/{assignmentId}", admin);
        one.GetProperty("data").GetProperty("isPrimary").GetBoolean().Should().BeFalse();

        // Ekipten çıkarılınca (eski rowVersion ile değil, güncel ile) erişim geri alınır; kayıt silinmez.
        var (sStale, _) = await _h.SendAsync(HttpMethod.Delete, $"/api/students/{studentA}/care-team/{assignmentId}", admin,
            new { rowVersion });
        sStale.Should().NotBe(HttpStatusCode.NoContent, "bayat rowVersion reddedilir");
        var current = one.GetProperty("data").GetProperty("rowVersion").GetInt32();
        var (sd, removed) = await _h.SendAsync(HttpMethod.Delete, $"/api/students/{studentA}/care-team/{assignmentId}", admin,
            new { rowVersion = current });
        sd.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        removed.ValueKind.Should().NotBe(JsonValueKind.Undefined);

        (await _h.SendAsync(HttpMethod.Get, $"/api/goals/student-goals?studentId={studentA}", educator)).Body.Pipe(Total).Should().Be(0);
        var (_, history) = await _h.SendAsync(HttpMethod.Get, $"/api/students/{studentA}/care-team?activeOnly=false", admin);
        history.GetProperty("data").EnumerateArray().Single().GetProperty("status").GetString().Should().Be("ended");
    }

    [Fact]
    public async Task Modules_me_follows_the_corporation_module_switch()
    {
        var admin = $"admin-mod-{_suffix}@example.test";
        var corp = await _h.NewTenantAsync($"mod_{_suffix}", admin);
        var token = TenantApiHarness.Token(await _h.LoginAsync(admin, TenantApiHarness.AdminPassword));
        static List<string> Codes(JsonElement b) =>
            b.GetProperty("data").GetProperty("enabledModules").EnumerateArray().Select(x => x.GetString()!).ToList();

        (await _h.SendAsync(HttpMethod.Get, "/api/modules/me")).Status.Should().Be(HttpStatusCode.Unauthorized);

        var (s1, closed) = await _h.SendAsync(HttpMethod.Get, "/api/modules/me", token);
        s1.Should().Be(HttpStatusCode.OK, closed.ToString());
        Codes(closed).Should().BeEmpty("yeni kurumda altı modül kapalı başlar");

        await _h.ExecAsync("update core.corporation set settings = jsonb_set(settings, '{modules,care_team}', 'true'::jsonb, true) where id = @c", ("c", corp));
        Codes((await _h.SendAsync(HttpMethod.Get, "/api/modules/me", token)).Body).Should().Equal("care_team");
    }

    [Fact]
    public async Task Without_the_restriction_the_care_team_does_not_hide_anything()
    {
        var (corp, _, educator, _, studentA, studentB) = await ArrangeAsync();
        await _h.ExecAsync("delete from core.setting_value where setting_key = 'care_team_abac_enabled' and corporation_id = @c", ("c", corp));

        (await _h.SendAsync(HttpMethod.Get, $"/api/goals/student-goals?studentId={studentA}", educator)).Body.Pipe(Total).Should().Be(1);
        (await _h.SendAsync(HttpMethod.Get, $"/api/goals/student-goals?studentId={studentB}", educator)).Body.Pipe(Total).Should().Be(1);
    }
}

file static class PipeExtensions
{
    public static TResult Pipe<T, TResult>(this T value, Func<T, TResult> f) => f(value);
}
