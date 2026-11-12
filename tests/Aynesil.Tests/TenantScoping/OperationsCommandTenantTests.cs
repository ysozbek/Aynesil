using Aynesil.Application.Features.Leaves.Commands;
using Aynesil.Application.Features.Meetings.Commands;
using Aynesil.Application.Features.Notifications.Commands;
using Aynesil.Application.Features.PerformanceKpi.Commands;
using Aynesil.Application.Features.Scheduling.Commands;
using Aynesil.Application.Features.Surveys.Commands;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Ops.Entities;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>Scheduling, leaves, meetings, notifications, KPI and surveys.</summary>
public class OperationsCommandTenantTests
{
    private static readonly DateTimeOffset At = new(2026, 12, 1, 9, 0, 0, TimeSpan.Zero);

    // ── Scheduling ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateRoom_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<CreateRoomCommandHandler>().Handle(
            new CreateRoomCommand("R1", "Derslik 1", 4, false, null, null, null), default);
        (await ctx.Db.Rooms.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateCalendarEntry_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<CreateCalendarEntryCommandHandler>().Handle(
            new CreateCalendarEntryCommand("Tatil", "holiday", At, At.AddDays(1), true, null), default);
        (await ctx.Db.CalendarEntries.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task RequestMakeup_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<RequestMakeupCommandHandler>().Handle(
            new RequestMakeupCommand(Guid.CreateVersion7(), null, null, null, null), default);
        (await ctx.Db.MakeupRequests.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateRecurringSchedule_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<CreateRecurringScheduleCommandHandler>().Handle(
            new CreateRecurringScheduleCommand("weekly", new TimeOnly(10, 0), 40, new DateOnly(2026, 12, 1),
                null, null, null, null, 1, [1], null, null, 4), default);
        (await ctx.Db.RecurringSchedules.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task ScheduleSession_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<ScheduleSessionCommandHandler>().Handle(
            new ScheduleSessionCommand(Guid.CreateVersion7(), At, At.AddMinutes(40), null, null, null, null,
                "Bireysel", false, [], []), default);
        (await ctx.Db.Sessions.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    // ── Leaves ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitLeaveRequest_uses_tenant_and_acting_user()
    {
        using var ctx = new TenantTestContext();
        var educator = Educator.Create(TenantA, "Zeynep", "Ak");
        await ctx.SeedAsync(educator);

        var id = await ctx.Handler<SubmitLeaveRequestCommandHandler>().Handle(LeaveCommand(educator.Id), default);

        var saved = await ctx.Db.LeaveRequests.SingleAsync(l => l.Id == id);
        saved.CorporationId.Should().Be(TenantA);
        saved.CreatedBy.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task SubmitLeaveRequest_rejects_educator_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var educator = Educator.Create(TenantB, "Zeynep", "Ak");
        await ctx.SeedAsync(educator);

        await ShouldRejectAsync(() => ctx.Handler<SubmitLeaveRequestCommandHandler>().Handle(LeaveCommand(educator.Id), default));
        (await ctx.Db.LeaveRequests.CountAsync()).Should().Be(0);
    }

    private static SubmitLeaveRequestCommand LeaveCommand(Guid educatorId) =>
        new(educatorId, null, "day", At, At.AddDays(1), 1m, "Yıllık izin");

    [Fact]
    public async Task InitializeLeaveBalance_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var id = await ctx.Handler<InitializeLeaveBalanceCommandHandler>().Handle(
            new InitializeLeaveBalanceCommand(Guid.CreateVersion7(), null, 2026, 14m), default);
        (await ctx.Db.LeaveBalances.SingleAsync(b => b.Id == id)).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CarryForwardLeaveBalance_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var educatorId = Guid.CreateVersion7();
        await ctx.SeedAsync(LeaveBalance.Initialize(TenantA, educatorId, null, 2026, 14m));

        await ctx.Handler<CarryForwardLeaveBalanceCommandHandler>().Handle(
            new CarryForwardLeaveBalanceCommand(educatorId, null, 2026, 2027), default);

        (await ctx.Db.LeaveBalances.SingleAsync(b => b.PeriodYear == 2027)).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CarryForwardLeaveBalance_rejects_balance_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var educatorId = Guid.CreateVersion7();
        await ctx.SeedAsync(LeaveBalance.Initialize(TenantB, educatorId, null, 2026, 14m));

        await ShouldRejectAsync(() => ctx.Handler<CarryForwardLeaveBalanceCommandHandler>().Handle(
            new CarryForwardLeaveBalanceCommand(educatorId, null, 2026, 2027), default));
        (await ctx.Db.LeaveBalances.CountAsync(b => b.PeriodYear == 2027)).Should().Be(0);
    }

    // ── Meetings ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ScheduleMeeting_uses_tenant_and_acting_user()
    {
        using var ctx = new TenantTestContext();
        var id = await ctx.Handler<ScheduleMeetingCommandHandler>().Handle(
            new ScheduleMeetingCommand("Veli görüşmesi", null, At, At.AddHours(1), null, null, null, null,
                [new ParticipantInput("external", null, null, null, "Dış katılımcı")]), default);

        var meeting = await ctx.Db.Meetings.SingleAsync(m => m.Id == id);
        meeting.CorporationId.Should().Be(TenantA);
        meeting.CreatedBy.Should().Be(ctx.UserId);
        (await ctx.Db.MeetingParticipants.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    // ── Notifications ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateNotificationTemplate_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<CreateNotificationTemplateCommandHandler>().Handle(
            new CreateNotificationTemplateCommand("SEANS_HATIRLATMA", null, null, [new TranslationInput("tr", "Konu", "Gövde")]), default);
        (await ctx.Db.NotificationTemplates.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task UpsertNotificationTriggerConfig_uses_tenant_and_acting_user()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<UpsertNotificationTriggerConfigCommandHandler>().Handle(
            new UpsertNotificationTriggerConfigCommand("session_reminder", null, 60, true, []), default);
        var saved = await ctx.Db.NotificationTriggerConfigs.SingleAsync();
        saved.CorporationId.Should().Be(TenantA);
        saved.CreatedBy.Should().Be(ctx.UserId);
    }

    // ── KPI ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateKpiDefinition_creates_a_tenant_definition_never_a_global_one()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<CreateKpiDefinitionCommandHandler>().Handle(
            new CreateKpiDefinitionCommand("ATT", "Devam oranı", null, "%", null), default);
        (await ctx.Db.KpiDefinitions.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task ComputePerformanceSnapshot_rejects_educator_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var educator = Educator.Create(TenantB, "Zeynep", "Ak");
        await ctx.SeedAsync(educator);

        await ShouldRejectAsync(() => ctx.Handler<ComputePerformanceSnapshotCommandHandler>().Handle(
            new ComputePerformanceSnapshotCommand(educator.Id, new(2026, 10, 1), new(2026, 10, 31)), default));
        (await ctx.Db.EducatorPerformanceSnapshots.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task BulkComputeSnapshots_only_targets_educators_of_the_tenant()
    {
        using var ctx = new TenantTestContext();
        await ctx.SeedAsync(Educator.Create(TenantA, "Zeynep", "Ak"), Educator.Create(TenantB, "Yabancı", "Eğitmen"));
        var sender = new Moq.Mock<MediatR.ISender>();
        var handler = new BulkComputeSnapshotsCommandHandler(ctx.Db, sender.Object, ctx.Tenant.Object);

        await handler.Handle(new BulkComputeSnapshotsCommand(new(2026, 10, 1), new(2026, 10, 31)), default);

        sender.Verify(s => s.Send(Moq.It.IsAny<ComputePerformanceSnapshotCommand>(), Moq.It.IsAny<CancellationToken>()),
            Moq.Times.Once, "only the tenant's single educator may be computed");
    }

    [Fact]
    public async Task SubmitParentFeedback_rejects_session_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var session = Session.Schedule(TenantB, Guid.CreateVersion7(), At, At.AddMinutes(40));
        await ctx.SeedAsync(session);

        await ShouldRejectAsync(() => ctx.Handler<SubmitParentFeedbackCommandHandler>().Handle(
            new SubmitParentFeedbackCommand(null, null, session.Id, 5, null), default));
        (await ctx.Db.ParentFeedbacks.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task SubmitParentFeedback_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<SubmitParentFeedbackCommandHandler>().Handle(
            new SubmitParentFeedbackCommand(null, null, null, 5, "Teşekkürler"), default);
        (await ctx.Db.ParentFeedbacks.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    // ── Surveys ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateSurveyDefinition_uses_tenant_and_acting_user()
    {
        using var ctx = new TenantTestContext();
        var id = await ctx.Handler<CreateSurveyDefinitionCommandHandler>().Handle(
            new CreateSurveyDefinitionCommand("Memnuniyet", "guardian", null, null,
                [new QuestionInput("Memnun musunuz?", "rating", true, 1, [])]), default);
        var saved = await ctx.Db.SurveyDefinitions.SingleAsync(s => s.Id == id);
        saved.CorporationId.Should().Be(TenantA);
        saved.CreatedBy.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task SubmitSurveyResponse_uses_tenant_and_respondent_from_context()
    {
        using var ctx = new TenantTestContext();
        var survey = SurveyDefinition.Create(TenantA, "Memnuniyet");
        await ctx.SeedAsync(survey);

        var id = await ctx.Handler<SubmitSurveyResponseCommandHandler>().Handle(
            new SubmitSurveyResponseCommand(survey.Id, null, null, null, []), default);

        var saved = await ctx.Db.SurveyResponses.SingleAsync(r => r.Id == id);
        saved.CorporationId.Should().Be(TenantA);
        saved.RespondentUserId.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task SubmitSurveyResponse_rejects_survey_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var survey = SurveyDefinition.Create(TenantB, "Memnuniyet");
        await ctx.SeedAsync(survey);

        await ShouldRejectAsync(() => ctx.Handler<SubmitSurveyResponseCommandHandler>().Handle(
            new SubmitSurveyResponseCommand(survey.Id, null, null, null, []), default));
        (await ctx.Db.SurveyResponses.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateParentFeedback_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<CreateParentFeedbackCommandHandler>().Handle(
            new CreateParentFeedbackCommand(null, null, null, 4, null), default);
        (await ctx.Db.ParentFeedbacks.SingleAsync()).CorporationId.Should().Be(TenantA);
    }
}
