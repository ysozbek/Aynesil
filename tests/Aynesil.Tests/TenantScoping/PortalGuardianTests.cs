using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Cameras.Commands;
using Aynesil.Application.Features.PerformanceKpi.Commands;
using Aynesil.Application.Features.Surveys.Commands;
using Aynesil.Domain.Modules.Legal.Entities;
using Aynesil.Domain.Modules.Media.Entities;
using Aynesil.Domain.Modules.Ops.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Portal requests: the guardian identity is derived from the signed-in user, never from the
/// payload, and a guardian cannot act with another guardian's records.
/// </summary>
public class PortalGuardianTests
{
    /// <summary>Guardian linked to the context's signed-in user, with portal access to a student.</summary>
    private static (Guardian Guardian, Guid StudentId, GuardianPortalAccess Access) SignedInGuardian(
        TenantTestContext ctx, bool canViewCamera = true)
    {
        var guardian = Guardian.Create(TenantA, "Ayşe", "Kaya");
        guardian.LinkPortalAccount(ctx.UserId);
        var studentId = Guid.CreateVersion7();
        var access = new GuardianPortalAccess
        {
            CorporationId = TenantA, GuardianId = guardian.Id, StudentId = studentId, CanViewCamera = canViewCamera,
        };
        return (guardian, studentId, access);
    }

    private static Guardian OtherGuardian() => Guardian.Create(TenantA, "Başka", "Veli");

    private static ViewingAuthorization Authorization(Guid guardianId, Guid studentId)
    {
        var now = DateTimeOffset.UtcNow;
        return ViewingAuthorization.Grant(TenantA, guardianId, studentId, now.AddHours(-1), now.AddDays(1));
    }

    // ── StartViewingSession ───────────────────────────────────────────────────

    [Fact]
    public async Task StartViewingSession_derives_guardian_from_signed_in_user()
    {
        using var ctx = new TenantTestContext();
        var (guardian, studentId, access) = SignedInGuardian(ctx);
        var auth = Authorization(guardian.Id, studentId);
        await ctx.SeedAsync(guardian, access, auth, StudentConsent.Grant(TenantA, studentId, guardian.Id));

        var result = await ctx.Handler<StartViewingSessionCommandHandler>().Handle(
            new StartViewingSessionCommand(auth.Id, null, null), default);

        (await ctx.Db.ViewingLogs.SingleAsync(l => l.Id == result.LogId)).GuardianId.Should().Be(guardian.Id);
    }

    [Fact]
    public async Task StartViewingSession_rejects_authorization_of_another_guardian()
    {
        using var ctx = new TenantTestContext();
        var (guardian, studentId, access) = SignedInGuardian(ctx);
        var other = OtherGuardian();
        var othersAuth = Authorization(other.Id, studentId);
        await ctx.SeedAsync(guardian, other, access, othersAuth, StudentConsent.Grant(TenantA, studentId, other.Id));

        var act = () => ctx.Handler<StartViewingSessionCommandHandler>().Handle(
            new StartViewingSessionCommand(othersAuth.Id, null, null), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        (await ctx.Db.ViewingLogs.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task StartViewingSession_rejects_guardian_without_camera_access()
    {
        using var ctx = new TenantTestContext();
        var (guardian, studentId, access) = SignedInGuardian(ctx, canViewCamera: false);
        var auth = Authorization(guardian.Id, studentId);
        await ctx.SeedAsync(guardian, access, auth, StudentConsent.Grant(TenantA, studentId, guardian.Id));

        var act = () => ctx.Handler<StartViewingSessionCommandHandler>().Handle(
            new StartViewingSessionCommand(auth.Id, null, null), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    // ── SubmitSurveyResponse ──────────────────────────────────────────────────

    [Fact]
    public async Task SubmitSurveyResponse_derives_guardian_from_signed_in_user()
    {
        using var ctx = new TenantTestContext();
        var (guardian, studentId, access) = SignedInGuardian(ctx);
        var survey = SurveyDefinition.Create(TenantA, "Memnuniyet");
        await ctx.SeedAsync(guardian, access, survey);

        var id = await ctx.Handler<SubmitSurveyResponseCommandHandler>().Handle(
            new SubmitSurveyResponseCommand(survey.Id, null, studentId, null, []), default);

        (await ctx.Db.SurveyResponses.SingleAsync(r => r.Id == id)).GuardianId.Should().Be(guardian.Id);
    }

    [Fact]
    public async Task SubmitSurveyResponse_rejects_another_guardians_id()
    {
        using var ctx = new TenantTestContext();
        var (guardian, _, access) = SignedInGuardian(ctx);
        var other = OtherGuardian();
        var survey = SurveyDefinition.Create(TenantA, "Memnuniyet");
        await ctx.SeedAsync(guardian, other, access, survey);

        var act = () => ctx.Handler<SubmitSurveyResponseCommandHandler>().Handle(
            new SubmitSurveyResponseCommand(survey.Id, other.Id, null, null, []), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        (await ctx.Db.SurveyResponses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SubmitSurveyResponse_rejects_student_of_another_guardian()
    {
        using var ctx = new TenantTestContext();
        var (guardian, _, access) = SignedInGuardian(ctx);
        var survey = SurveyDefinition.Create(TenantA, "Memnuniyet");
        await ctx.SeedAsync(guardian, access, survey);

        var act = () => ctx.Handler<SubmitSurveyResponseCommandHandler>().Handle(
            new SubmitSurveyResponseCommand(survey.Id, null, Guid.CreateVersion7(), null, []), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        (await ctx.Db.SurveyResponses.CountAsync()).Should().Be(0);
    }

    // ── Parent feedback (both endpoints) ──────────────────────────────────────

    [Fact]
    public async Task SubmitParentFeedback_derives_guardian_from_signed_in_user()
    {
        using var ctx = new TenantTestContext();
        var (guardian, _, access) = SignedInGuardian(ctx);
        await ctx.SeedAsync(guardian, access);

        var dto = await ctx.Handler<SubmitParentFeedbackCommandHandler>().Handle(
            new SubmitParentFeedbackCommand(null, null, null, 5, null), default);

        dto.GuardianId.Should().Be(guardian.Id);
    }

    [Fact]
    public async Task SubmitParentFeedback_rejects_another_guardians_id()
    {
        using var ctx = new TenantTestContext();
        var (guardian, _, access) = SignedInGuardian(ctx);
        var other = OtherGuardian();
        await ctx.SeedAsync(guardian, other, access);

        var act = () => ctx.Handler<SubmitParentFeedbackCommandHandler>().Handle(
            new SubmitParentFeedbackCommand(other.Id, null, null, 5, null), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        (await ctx.Db.ParentFeedbacks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateParentFeedback_rejects_another_guardians_id()
    {
        using var ctx = new TenantTestContext();
        var (guardian, _, access) = SignedInGuardian(ctx);
        var other = OtherGuardian();
        await ctx.SeedAsync(guardian, other, access);

        var act = () => ctx.Handler<CreateParentFeedbackCommandHandler>().Handle(
            new CreateParentFeedbackCommand(other.Id, null, null, 5, null), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
        (await ctx.Db.ParentFeedbacks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Staff_may_submit_feedback_for_a_guardian_of_the_same_tenant_only()
    {
        using var ctx = new TenantTestContext();   // signed-in user is not linked to any guardian
        var foreignGuardian = Guardian.Create(TenantB, "Yabancı", "Veli");
        await ctx.SeedAsync(foreignGuardian);

        await ShouldRejectAsync(() => ctx.Handler<CreateParentFeedbackCommandHandler>().Handle(
            new CreateParentFeedbackCommand(foreignGuardian.Id, null, null, 5, null), default));
        (await ctx.Db.ParentFeedbacks.CountAsync()).Should().Be(0);
    }
}
