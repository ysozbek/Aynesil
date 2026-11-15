using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace Aynesil.IntegrationTests;

/// <summary>
/// Bakım ekibi ön filtresi (CareTeamFilter) veritabanındaki kuralla aynıdır (students.user_can_access_student, V16):
/// kurum ABAC'ı açmadıkça (core.setting_value care_team_abac_enabled, varsayılan kapalı) yalnızca RBAC geçerlidir.
/// Önceden ön filtre bu anahtarı yok sayıyordu: care_team:bypass izni olmayan kullanıcı (ör. Eğitmen) öğrenci bazlı
/// hedef listesinde hiçbir şey görmüyor, seansa hedef ekleyemiyordu. ABAC açılınca atanmamış öğrenci yine gizlenir.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class CareTeamPreFilterTests : IDisposable
{
    private const string UserPassword = "Egitmen#Sifre1";
    private readonly TenantApiHarness _h;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public CareTeamPreFilterTests(MigratedDatabaseFixture db) => _h = new TenantApiHarness(db);

    public void Dispose() => _h.Dispose();

    private static int Total(JsonElement body) => body.GetProperty("data").GetProperty("totalCount").GetInt32();

    [Fact]
    public async Task Goals_by_student_follow_the_tenant_abac_switch()
    {
        var admin = $"admin-ct-{_suffix}@example.test";
        var corp = await _h.NewTenantAsync($"ct_{_suffix}", admin);
        var adminToken = TenantApiHarness.Token(await _h.LoginAsync(admin, TenantApiHarness.AdminPassword));

        // Öğrenci + hedef (sahip rolüyle; konu ön filtre).
        var student = Guid.NewGuid();
        await _h.ExecAsync("insert into students.student(id, corporation_id, first_name, last_name) values (@id, @c, 'Ada', 'Test')",
            ("id", student), ("c", corp));
        await _h.ExecAsync("insert into education.student_goal(corporation_id, student_id, statement) values (@c, @s, 'Göz teması')",
            ("c", corp), ("s", student));

        // Yalnızca student_goal:read içeren rol (care_team:bypass yok) — eğitmen gibi.
        var (rs, role) = await _h.SendAsync(HttpMethod.Post, "/api/roles", adminToken,
            new { code = $"goalreader_{_suffix}", name = "Hedef okuyucu", description = "test" });
        rs.Should().Be(HttpStatusCode.Created, role.ToString());
        var roleId = role.GetProperty("data").GetProperty("id").GetGuid();
        var permissionId = await _h.ScalarAsync<Guid>("select id from iam.permission where code = 'student_goal:read'");
        (await _h.SendAsync(HttpMethod.Post, $"/api/roles/{roleId}/permissions", adminToken, new { permissionId }))
            .Status.Should().Be(HttpStatusCode.Created);
        var username = $"egitmen-ct-{_suffix}@example.test";
        var userId = await _h.CreateUserAsync(adminToken, username, UserPassword);
        (await _h.SendAsync(HttpMethod.Post, $"/api/users/{userId}/roles", adminToken, new { roleId, campusId = (Guid?)null }))
            .Status.Should().Be(HttpStatusCode.Created);
        var token = TenantApiHarness.Token(await _h.LoginAsync(username, UserPassword));

        // RBAC-only (varsayılan): hedef görünür.
        var (s1, rbac) = await _h.SendAsync(HttpMethod.Get, $"/api/goals/student-goals?studentId={student}", token);
        s1.Should().Be(HttpStatusCode.OK, rbac.ToString());
        Total(rbac).Should().Be(1);

        // Kurum ABAC'ı açar: bakım ekibine atanmamış kullanıcı öğrenciyi göremez.
        await _h.ExecAsync(
            "insert into core.setting_value(setting_key, scope_level, corporation_id, value) values ('care_team_abac_enabled', 'corporation', @c, 'true')",
            ("c", corp));
        var (s2, abac) = await _h.SendAsync(HttpMethod.Get, $"/api/goals/student-goals?studentId={student}", token);
        s2.Should().Be(HttpStatusCode.OK);
        Total(abac).Should().Be(0);
    }
}
