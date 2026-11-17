using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace Aynesil.IntegrationTests;

/// <summary>
/// docs/raporlar/2026-11-17_dogrulama.md D7 (V64 uq_survey_response_once), gerçek PostgreSQL üzerinde: aynı yanıtın
/// eşzamanlı gönderiminden yalnızca biri yazılır; diğerleri 409 <c>Rule.SurveyAlreadySubmitted</c> alır — handler'ın
/// ön kontrolünü yarışta geçen istek de (tekil indeks ihlali 23505 → aynı kod), genel "kayıt zaten var" değil.
/// </summary>
[Collection(MigratedDatabaseCollection.Name)]
[Trait("Category", "Integration")]
public sealed class SurveyResponseOnceTests : IDisposable
{
    private readonly TenantApiHarness _h;
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public SurveyResponseOnceTests(MigratedDatabaseFixture db) => _h = new TenantApiHarness(db);

    public void Dispose() => _h.Dispose();

    [Fact]
    public async Task Concurrent_identical_submissions_store_one_response()
    {
        var corp = await _h.NewTenantAsync($"so_{_suffix}", $"admin-so-{_suffix}@example.test");
        await _h.ExecAsync(
            "update core.corporation set settings = jsonb_set(coalesce(settings, '{}'::jsonb), '{modules}', '{\"surveys\": true}'::jsonb, true) where id = @c",
            ("c", corp));
        var token = TenantApiHarness.Token(await _h.LoginAsync($"admin-so-{_suffix}@example.test", TenantApiHarness.AdminPassword));

        var (cs, created) = await _h.SendAsync(HttpMethod.Post, "/api/surveys", token, new
        {
            title = "Eşzamanlı Anket", target = "educator", typeId = (Guid?)null, description = (string?)null,
            questions = new[] { new { questionText = "Öneri", questionType = "text", isRequired = false, sortOrder = 1, answerOptions = Array.Empty<object>() } },
        });
        cs.Should().Be(HttpStatusCode.Created, created.ToString());
        var data = created.GetProperty("data");
        var surveyId = data.ValueKind == JsonValueKind.String ? data.GetGuid() : data.GetProperty("id").GetGuid();
        var questionId = await _h.ScalarAsync<Guid>("select id from ops.survey_question where survey_id = @s", ("s", surveyId));

        var body = new
        {
            surveyId, guardianId = (Guid?)null, studentId = (Guid?)null, sessionId = (Guid?)null,
            answers = new[] { new { questionId, answerText = "aynı yanıt", answerOptionId = (Guid?)null, numericValue = (decimal?)null } },
        };
        var results = await Task.WhenAll(Enumerable.Range(0, 6)
            .Select(_ => _h.SendAsync(HttpMethod.Post, "/api/surveys/responses", token, body)));

        results.Count(r => r.Status == HttpStatusCode.Created).Should().Be(1, string.Join(" | ", results.Select(r => (int)r.Status)));
        results.Where(r => r.Status != HttpStatusCode.Created).Should().OnlyContain(r =>
            r.Status == HttpStatusCode.Conflict && r.Body.GetProperty("code").GetString() == "Rule.SurveyAlreadySubmitted");
        (await _h.ScalarAsync<long>("select count(*) from ops.survey_response where survey_id = @s and submitted_at is not null", ("s", surveyId)))
            .Should().Be(1);
    }
}
