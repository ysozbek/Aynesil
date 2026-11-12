using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Cameras.Commands;
using Aynesil.Application.Features.Leads.Commands;
using Aynesil.Domain.Modules.Crm.Entities;
using Aynesil.Domain.Modules.Iam.Entities;
using Aynesil.Domain.Modules.Legal.Entities;
using Aynesil.Domain.Modules.Media.Entities;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// #86: ids that the client still chooses (ConductedBy, PerformedBy, the camera/session of a
/// viewing session) must point at records of the current tenant.
/// </summary>
public class ClientChosenReferenceTests
{
    private static UserAccount User(Guid corp) =>
        UserAccount.Create(corp, $"user-{Guid.NewGuid():N}@test.local", "Personel", activateImmediately: true);

    // ── CompleteInterview.ConductedBy ─────────────────────────────────────────

    [Fact]
    public async Task CompleteInterview_accepts_a_user_of_the_tenant()
    {
        using var ctx = new TenantTestContext();
        var interviewer = User(TenantA);
        var interview = Interview.Schedule(TenantA, Guid.CreateVersion7(), null, null);
        await ctx.SeedAsync(interviewer, interview);

        await ctx.Handler<CompleteInterviewCommandHandler>().Handle(
            new CompleteInterviewCommand(interview.Id, "Olumlu", null, interviewer.Id, interview.RowVersion), default);

        (await ctx.Db.Interviews.SingleAsync()).ConductedBy.Should().Be(interviewer.Id);
    }

    [Fact]
    public async Task CompleteInterview_rejects_a_user_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var foreignUser = User(TenantB);
        var interview = Interview.Schedule(TenantA, Guid.CreateVersion7(), null, null);
        await ctx.SeedAsync(foreignUser, interview);

        await ShouldRejectAsync(() => ctx.Handler<CompleteInterviewCommandHandler>().Handle(
            new CompleteInterviewCommand(interview.Id, "Olumlu", null, foreignUser.Id, interview.RowVersion), default));
        (await ctx.Db.Interviews.SingleAsync()).ConductedBy.Should().BeNull();
    }

    // ── LogLeadActivity.PerformedBy ───────────────────────────────────────────

    [Fact]
    public async Task LogLeadActivity_accepts_a_user_of_the_tenant()
    {
        using var ctx = new TenantTestContext();
        var colleague = User(TenantA);
        var lead = Lead.Create(TenantA, "Veli");
        await ctx.SeedAsync(colleague, lead);

        await ctx.Handler<LogLeadActivityCommandHandler>().Handle(ActivityCommand(lead.Id, colleague.Id), default);

        (await ctx.Db.LeadActivities.SingleAsync()).PerformedBy.Should().Be(colleague.Id);
    }

    [Fact]
    public async Task LogLeadActivity_rejects_a_user_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var foreignUser = User(TenantB);
        var lead = Lead.Create(TenantA, "Veli");
        await ctx.SeedAsync(foreignUser, lead);

        await ShouldRejectAsync(() => ctx.Handler<LogLeadActivityCommandHandler>().Handle(
            ActivityCommand(lead.Id, foreignUser.Id), default));
        (await ctx.Db.LeadActivities.CountAsync()).Should().Be(0);
    }

    private static LogLeadActivityCommand ActivityCommand(Guid leadId, Guid performedBy) =>
        new(leadId, null, "Arama", null, "outbound", DateTimeOffset.UtcNow, null, performedBy);

    // ── StartViewingSession camera / session ──────────────────────────────────

    [Fact]
    public async Task StartViewingSession_rejects_a_camera_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var (auth, consent) = ValidAuthorization(sessionId: null);
        var foreignCamera = Camera.Register(TenantB, "CAM-B", "Yabancı");
        await ctx.SeedAsync(auth, consent, foreignCamera);

        await ShouldRejectAsync(() => ctx.Handler<StartViewingSessionCommandHandler>().Handle(
            new StartViewingSessionCommand(auth.Id, null, foreignCamera.Id), default));
        (await ctx.Db.ViewingLogs.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task StartViewingSession_rejects_a_session_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var (auth, consent) = ValidAuthorization(sessionId: null);
        var start = DateTimeOffset.UtcNow;
        var foreignSession = Session.Schedule(TenantB, Guid.CreateVersion7(), start, start.AddMinutes(40));
        await ctx.SeedAsync(auth, consent, foreignSession);

        await ShouldRejectAsync(() => ctx.Handler<StartViewingSessionCommandHandler>().Handle(
            new StartViewingSessionCommand(auth.Id, foreignSession.Id, null), default));
        (await ctx.Db.ViewingLogs.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task StartViewingSession_rejects_a_session_other_than_the_authorized_one()
    {
        using var ctx = new TenantTestContext();
        var start = DateTimeOffset.UtcNow;
        var authorizedSession = Session.Schedule(TenantA, Guid.CreateVersion7(), start, start.AddMinutes(40));
        var otherSession = Session.Schedule(TenantA, Guid.CreateVersion7(), start.AddHours(2), start.AddHours(3));
        var (auth, consent) = ValidAuthorization(authorizedSession.Id);
        await ctx.SeedAsync(authorizedSession, otherSession, auth, consent);

        var act = () => ctx.Handler<StartViewingSessionCommandHandler>().Handle(
            new StartViewingSessionCommand(auth.Id, otherSession.Id, null), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task StartViewingSession_accepts_camera_and_session_of_the_tenant()
    {
        using var ctx = new TenantTestContext();
        var start = DateTimeOffset.UtcNow;
        var session = Session.Schedule(TenantA, Guid.CreateVersion7(), start, start.AddMinutes(40));
        var camera = Camera.Register(TenantA, "CAM-A", "Kamera");
        var (auth, consent) = ValidAuthorization(session.Id);
        await ctx.SeedAsync(session, camera, auth, consent);

        var result = await ctx.Handler<StartViewingSessionCommandHandler>().Handle(
            new StartViewingSessionCommand(auth.Id, session.Id, camera.Id), default);

        var log = await ctx.Db.ViewingLogs.SingleAsync(l => l.Id == result.LogId);
        log.CameraId.Should().Be(camera.Id);
        log.SessionId.Should().Be(session.Id);
    }

    private static (ViewingAuthorization Auth, StudentConsent Consent) ValidAuthorization(Guid? sessionId)
    {
        var now = DateTimeOffset.UtcNow;
        var guardianId = Guid.CreateVersion7();
        var studentId = Guid.CreateVersion7();
        var auth = ViewingAuthorization.Grant(TenantA, guardianId, studentId, now.AddHours(-1), now.AddDays(1), sessionId);
        return (auth, StudentConsent.Grant(TenantA, studentId, guardianId));
    }
}
