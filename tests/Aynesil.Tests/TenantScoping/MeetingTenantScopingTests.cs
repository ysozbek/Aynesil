using Aynesil.Application.Features.Meetings.Commands;
using Aynesil.Domain.Modules.Ops.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

public class MeetingTenantScopingTests
{
    // ── AddMeetingParticipant ─────────────────────────────────────────────────

    [Fact]
    public async Task AddMeetingParticipant_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var meeting = Meeting.Create(TenantA, "Veli görüşmesi");
        await ctx.SeedAsync(meeting);

        var handler = new AddMeetingParticipantCommandHandler(ctx.Db, ctx.Tenant.Object);
        var dto = await handler.Handle(ParticipantCommand(meeting.Id), default);

        dto.CorporationId.Should().Be(TenantA);
        (await ctx.Db.MeetingParticipants.SingleAsync(p => p.Id == dto.Id)).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task AddMeetingParticipant_rejects_meeting_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var foreignMeeting = Meeting.Create(TenantB, "Yabancı toplantı");
        await ctx.SeedAsync(foreignMeeting);

        var handler = new AddMeetingParticipantCommandHandler(ctx.Db, ctx.Tenant.Object);
        var act = () => handler.Handle(ParticipantCommand(foreignMeeting.Id), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        (await ctx.Db.MeetingParticipants.CountAsync()).Should().Be(0);
    }

    // ── RecordMeetingOutcome ──────────────────────────────────────────────────

    [Fact]
    public async Task RecordMeetingOutcome_uses_tenant_and_acting_user_from_context()
    {
        using var ctx = new TenantTestContext();
        var meeting = Meeting.Create(TenantA, "Veli görüşmesi");
        await ctx.SeedAsync(meeting);

        var handler = new RecordMeetingOutcomeCommandHandler(
            ctx.Db, ctx.Tenant.Object, ctx.CurrentUser.Object);
        var dto = await handler.Handle(
            new RecordMeetingOutcomeCommand(meeting.Id, "Özet", null), default);

        var saved = await ctx.Db.MeetingOutcomes.SingleAsync(o => o.Id == dto.Id);
        saved.CorporationId.Should().Be(TenantA);
        saved.CreatedBy.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task RecordMeetingOutcome_rejects_meeting_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var foreignMeeting = Meeting.Create(TenantB, "Yabancı toplantı");
        await ctx.SeedAsync(foreignMeeting);

        var handler = new RecordMeetingOutcomeCommandHandler(
            ctx.Db, ctx.Tenant.Object, ctx.CurrentUser.Object);
        var act = () => handler.Handle(
            new RecordMeetingOutcomeCommand(foreignMeeting.Id, "Özet", null), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        (await ctx.Db.MeetingOutcomes.CountAsync()).Should().Be(0);
    }

    // ── AddMeetingFollowUp ────────────────────────────────────────────────────

    [Fact]
    public async Task AddMeetingFollowUp_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var meeting = Meeting.Create(TenantA, "Veli görüşmesi");
        await ctx.SeedAsync(meeting);

        var handler = new AddMeetingFollowUpCommandHandler(ctx.Db, ctx.Tenant.Object);
        var dto = await handler.Handle(
            new AddMeetingFollowUpCommand(meeting.Id, "Rapor gönder", null, null), default);

        (await ctx.Db.MeetingFollowUps.SingleAsync(f => f.Id == dto.Id)).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task AddMeetingFollowUp_rejects_meeting_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var foreignMeeting = Meeting.Create(TenantB, "Yabancı toplantı");
        await ctx.SeedAsync(foreignMeeting);

        var handler = new AddMeetingFollowUpCommandHandler(ctx.Db, ctx.Tenant.Object);
        var act = () => handler.Handle(
            new AddMeetingFollowUpCommand(foreignMeeting.Id, "Rapor gönder", null, null), default);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        (await ctx.Db.MeetingFollowUps.CountAsync()).Should().Be(0);
    }

    private static AddMeetingParticipantCommand ParticipantCommand(Guid meetingId) =>
        new(meetingId, "external", null, null, null, "Dış katılımcı");
}
