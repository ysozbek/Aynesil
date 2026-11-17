using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace Aynesil.IntegrationTests;

/// <summary>
/// docs/raporlar/2026-11-17_dogrulama.md D6 ve D3, gerçek PostgreSQL üzerinde:
///   - kontenjanı 1 olan döneme aynı anda gelen kayıtlardan yalnızca biri 'enrolled' olur (dönem satırı
///     SELECT … FOR UPDATE ile kilitlenir; önceden "say, sonra ekle" yarışında kontenjan aşılabiliyordu);
///   - çekilmiş öğrenci aynı döneme yeniden kaydedilir: (camp_period_id, student_id) tekil kısıtına takılmadan
///     aynı satır geri açılır (önceden genel 409 Error.DuplicateKey dönüyordu).
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class CampEnrollmentConcurrencyTests : IDisposable
{
    private readonly TenantApiHarness _h;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public CampEnrollmentConcurrencyTests(MigratedDatabaseFixture db) => _h = new TenantApiHarness(db);

    public void Dispose() => _h.Dispose();

    private async Task<(string Token, Guid PeriodId)> SetupAsync(int capacity)
    {
        var corp = await _h.NewTenantAsync($"ce_{_suffix}", $"admin-ce-{_suffix}@example.test");
        await _h.ExecAsync(
            "update core.corporation set settings = jsonb_set(coalesce(settings, '{}'::jsonb), '{modules}', '{\"camps\": true}'::jsonb, true) where id = @c",
            ("c", corp));
        var token = TenantApiHarness.Token(await _h.LoginAsync($"admin-ce-{_suffix}@example.test", TenantApiHarness.AdminPassword));

        var (cs, camp) = await _h.SendAsync(HttpMethod.Post, "/api/camps", token,
            new { campusId = (Guid?)null, code = $"K-{_suffix}", name = "Yarış Kampı", description = (string?)null, location = (string?)null, capacity = (int?)null });
        cs.Should().Be(HttpStatusCode.Created, camp.ToString());
        var campId = IdOf(camp);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var (ps, period) = await _h.SendAsync(HttpMethod.Post, $"/api/camps/{campId}/periods", token,
            new { name = "Dönem", startDate = today.AddDays(5).ToString("yyyy-MM-dd"), endDate = today.AddDays(15).ToString("yyyy-MM-dd"), capacity });
        ps.Should().Be(HttpStatusCode.Created, period.ToString());
        return (token, IdOf(period));
    }

    private async Task<Guid> NewStudentAsync(string token, int i)
    {
        var (s, body) = await _h.SendAsync(HttpMethod.Post, "/api/students", token,
            new { firstName = "Öğrenci", lastName = $"{i}-{_suffix}", studentNo = $"S{i}-{_suffix}", birthDate = "2016-05-01" });
        s.Should().Be(HttpStatusCode.Created, body.ToString());
        return IdOf(body);
    }

    private static Guid IdOf(JsonElement body)
    {
        var data = body.GetProperty("data");
        return data.ValueKind == JsonValueKind.String ? data.GetGuid() : data.GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task Concurrent_enrollments_never_exceed_the_capacity()
    {
        var (token, periodId) = await SetupAsync(capacity: 1);
        var students = new List<Guid>();
        for (var i = 0; i < 6; i++) students.Add(await NewStudentAsync(token, i));

        var results = await Task.WhenAll(students.Select(sid =>
            _h.SendAsync(HttpMethod.Post, $"/api/camps/periods/{periodId}/enrollments", token, new { studentId = sid, status = "enrolled" })));

        results.Count(r => r.Status == HttpStatusCode.Created).Should().Be(1, string.Join(" | ", results.Select(r => $"{(int)r.Status}")));
        results.Where(r => r.Status != HttpStatusCode.Created)
            .Should().OnlyContain(r => r.Status == HttpStatusCode.Conflict
                                       && r.Body.GetProperty("code").GetString() == "Rule.CampPeriodFullUseWaitlist");
        (await _h.ScalarAsync<long>(
            "select count(*) from camps.camp_enrollment where camp_period_id = @p and status = 'enrolled'", ("p", periodId)))
            .Should().Be(1);
    }

    [Fact]
    public async Task Withdrawn_student_is_re_enrolled_on_the_same_row_and_falls_to_waitlist_when_full()
    {
        var (token, periodId) = await SetupAsync(capacity: 1);
        var a = await NewStudentAsync(token, 1);
        var b = await NewStudentAsync(token, 2);

        var (_, first) = await _h.SendAsync(HttpMethod.Post, $"/api/camps/periods/{periodId}/enrollments", token, new { studentId = a, status = "enrolled" });
        var enrollmentId = IdOf(first);
        (await _h.SendAsync(HttpMethod.Post, $"/api/camps/enrollments/{enrollmentId}/withdraw", token, new { })).Status.Should().Be(HttpStatusCode.OK);
        (await _h.SendAsync(HttpMethod.Post, $"/api/camps/periods/{periodId}/enrollments", token, new { studentId = b, status = "enrolled" }))
            .Status.Should().Be(HttpStatusCode.Created);   // B tek koltuğu aldı

        var (status, again) = await _h.SendAsync(HttpMethod.Post, $"/api/camps/periods/{periodId}/enrollments", token, new { studentId = a, status = "enrolled" });
        status.Should().Be(HttpStatusCode.Created, again.ToString());
        IdOf(again).Should().Be(enrollmentId);
        again.GetProperty("data").GetProperty("status").GetString().Should().Be("waitlist");
        (await _h.ScalarAsync<long>("select count(*) from camps.camp_enrollment where camp_period_id = @p and student_id = @s",
            ("p", periodId), ("s", a))).Should().Be(1);
    }
}
