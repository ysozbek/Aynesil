using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Assessment.Commands;
using Aynesil.Application.Features.Finance.Commands;
using Aynesil.Application.Features.Meetings.Commands;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Assessment.Entities;
using Aynesil.Domain.Modules.Finance.Entities;
using Aynesil.Domain.Modules.Ops.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>Toplantı sonucu, değerlendirme önerisi ve burs: düzenleme komutları.</summary>
public class DetailEditCommandTests
{
    // ── Toplantı sonucu ─────────────────────────────────────────────────────

    [Fact]
    public async Task MeetingOutcome_update_replaces_summary_and_decisions()
    {
        using var ctx = new TenantTestContext();
        var outcome = MeetingOutcome.Create(TenantA, Guid.CreateVersion7(), "Eski özet", "Eski karar");
        await ctx.SeedAsync(outcome);

        await ctx.Handler<UpdateMeetingOutcomeCommandHandler>().Handle(
            new UpdateMeetingOutcomeCommand(outcome.Id, "Yeni özet", "Yeni karar"), default);

        var saved = await ctx.Db.MeetingOutcomes.SingleAsync();
        saved.Summary.Should().Be("Yeni özet");
        saved.Decisions.Should().Be("Yeni karar");
    }

    [Fact]
    public async Task MeetingOutcome_update_of_an_unknown_outcome_is_not_found_and_empty_text_is_invalid()
    {
        using var ctx = new TenantTestContext();
        var act = () => ctx.Handler<UpdateMeetingOutcomeCommandHandler>().Handle(
            new UpdateMeetingOutcomeCommand(Guid.CreateVersion7(), "x", null), default);
        await act.Should().ThrowAsync<NotFoundException>();

        new UpdateMeetingOutcomeCommandValidator()
            .Validate(new UpdateMeetingOutcomeCommand(Guid.CreateVersion7(), " ", null)).IsValid.Should().BeFalse();
    }

    // ── Değerlendirme önerisi ───────────────────────────────────────────────

    private static ProgramRecommendation Rec(Guid sessionId, Guid programId)
        => ProgramRecommendation.Create(TenantA, sessionId, null, Guid.CreateVersion7(), programId, "Haftada 2", "İlk gerekçe", Guid.CreateVersion7());

    [Fact]
    public async Task Recommendation_update_changes_intensity_and_rationale_and_keeps_program()
    {
        using var ctx = new TenantTestContext();
        var sessionId = Guid.CreateVersion7();
        var programId = Guid.CreateVersion7();
        var rec = Rec(sessionId, programId);
        await ctx.SeedAsync(rec);

        var dto = await ctx.Handler<UpdateProgramRecommendationCommandHandler>().Handle(
            new UpdateProgramRecommendationCommand(rec.Id, programId, "Haftada 3", "Güncel gerekçe", rec.RecommendedBy, rec.RowVersion, sessionId), default);

        dto.RecommendedIntensity.Should().Be("Haftada 3");
        dto.Rationale.Should().Be("Güncel gerekçe");
        dto.RecommendedProgramId.Should().Be(programId);
    }

    [Fact]
    public async Task Recommendation_update_with_a_stale_row_version_is_a_concurrency_error()
    {
        using var ctx = new TenantTestContext();
        var sessionId = Guid.CreateVersion7();
        var rec = Rec(sessionId, Guid.CreateVersion7());
        await ctx.SeedAsync(rec);

        var act = () => ctx.Handler<UpdateProgramRecommendationCommandHandler>().Handle(
            new UpdateProgramRecommendationCommand(rec.Id, null, "x", null, null, rec.RowVersion + 5, sessionId), default);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.ProgramRecommendations.SingleAsync()).RecommendedIntensity.Should().Be("Haftada 2");
    }

    [Fact]
    public async Task Recommendation_update_through_another_assessment_session_is_not_found()
    {
        using var ctx = new TenantTestContext();
        var rec = Rec(Guid.CreateVersion7(), Guid.CreateVersion7());
        await ctx.SeedAsync(rec);

        var act = () => ctx.Handler<UpdateProgramRecommendationCommandHandler>().Handle(
            new UpdateProgramRecommendationCommand(rec.Id, null, "x", null, null, rec.RowVersion, Guid.CreateVersion7()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── Burs ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Scholarship_update_changes_values_and_a_stale_row_version_is_rejected()
    {
        using var ctx = new TenantTestContext();
        var sch = Scholarship.Grant(TenantA, Guid.CreateVersion7(), percentage: 10m, validFrom: new DateOnly(2026, 9, 1));
        await ctx.SeedAsync(sch);
        var handler = ctx.Handler<UpdateScholarshipCommandHandler>();

        var dto = await handler.Handle(new UpdateScholarshipCommand(
            sch.Id, null, null, 500m, new DateOnly(2026, 9, 1), new DateOnly(2027, 6, 30), "Not", sch.RowVersion), default);
        dto.Amount.Should().Be(500m);
        dto.Percentage.Should().BeNull();
        dto.Note.Should().Be("Not");

        var act = () => handler.Handle(new UpdateScholarshipCommand(
            sch.Id, null, 20m, null, null, null, null, sch.RowVersion + 9), default);
        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public void Scholarship_update_validator_enforces_the_same_rules_as_grant()
    {
        var v = new UpdateScholarshipCommandValidator();
        var id = Guid.CreateVersion7();
        v.Validate(new UpdateScholarshipCommand(id, null, 10m, 100m, null, null, null, 1)).IsValid.Should().BeFalse("yüzde ve tutar birlikte olamaz");
        v.Validate(new UpdateScholarshipCommand(id, null, 10m, null, new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 1), null, 1)).IsValid.Should().BeFalse("bitiş başlangıçtan önce");
        v.Validate(new UpdateScholarshipCommand(id, null, 10m, null, null, null, new string('x', 1001), 1)).IsValid.Should().BeFalse("not çok uzun");
        v.Validate(new UpdateScholarshipCommand(id, null, 10m, null, new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1), "ok", 1)).IsValid.Should().BeTrue();
    }
}
