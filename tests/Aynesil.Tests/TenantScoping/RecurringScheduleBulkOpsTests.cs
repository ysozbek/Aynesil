using Aynesil.Application.Common.Exceptions;
using Aynesil.Domain.Common;
using Aynesil.Application.Features.Scheduling.Commands;
using Aynesil.Domain.Interfaces.Repositories;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>Tekrar eden plan: toplu iptal, toplu oda değişikliği, tek tarih istisnası.</summary>
public class RecurringScheduleBulkOpsTests
{
    private static readonly DateTimeOffset Future = DateTimeOffset.UtcNow.Date.AddDays(10).AddHours(10);

    private static RecurringSchedule Plan(Guid? roomId = null) => RecurringSchedule.Create(
        TenantA, "weekly", new TimeOnly(10, 0), 40, DateOnly.FromDateTime(DateTime.UtcNow), null,
        null, Guid.CreateVersion7(), roomId, 1, [1], null, null, null, Guid.CreateVersion7());

    private static Session InPlan(RecurringSchedule plan, int dayOffset, Guid? roomId = null)
        => Session.Schedule(TenantA, Guid.CreateVersion7(), Future.AddDays(dayOffset), Future.AddDays(dayOffset).AddMinutes(40),
            roomId: roomId, recurringScheduleId: plan.Id);

    // ── Toplu iptal ─────────────────────────────────────────────────────────

    [Fact]
    public async Task BulkCancel_cancels_only_upcoming_sessions_in_range_with_the_reason()
    {
        using var ctx = new TenantTestContext();
        var plan = Plan();
        var inRange = InPlan(plan, 0);
        var postponed = InPlan(plan, 1);
        postponed.Reschedule(postponed.StartsAt.AddHours(1), postponed.EndsAt.AddHours(1), null); // "rescheduled" da iptal edilir
        var outside = InPlan(plan, 20);
        var past = Session.Schedule(TenantA, Guid.CreateVersion7(), DateTimeOffset.UtcNow.AddDays(-3), DateTimeOffset.UtcNow.AddDays(-3).AddMinutes(40), recurringScheduleId: plan.Id);
        await ctx.SeedAsync(plan, inRange, postponed, outside, past);

        var from = DateOnly.FromDateTime(Future.UtcDateTime);
        var result = await ctx.Handler<BulkCancelSessionsCommandHandler>().Handle(
            new BulkCancelSessionsCommand(plan.Id, "Okul tatili", from, from.AddDays(5)), default);

        result.AffectedCount.Should().Be(2);
        var byId = await ctx.Db.Sessions.ToDictionaryAsync(s => s.Id);
        byId[inRange.Id].Status.Should().Be("cancelled");
        byId[inRange.Id].CancelReason.Should().Be("Okul tatili");
        byId[postponed.Id].Status.Should().Be("cancelled");
        byId[outside.Id].Status.Should().Be("scheduled");
        byId[past.Id].Status.Should().Be("scheduled");
    }

    [Fact]
    public async Task BulkCancel_for_an_unknown_plan_is_not_found()
    {
        using var ctx = new TenantTestContext();
        var act = () => ctx.Handler<BulkCancelSessionsCommandHandler>().Handle(
            new BulkCancelSessionsCommand(Guid.CreateVersion7(), "x", null, null), default);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public void BulkCancel_validator_rejects_an_inverted_range()
    {
        var v = new BulkCancelSessionsCommandValidator();
        var d = new DateOnly(2027, 1, 10);
        v.Validate(new BulkCancelSessionsCommand(Guid.CreateVersion7(), "x", d, d.AddDays(-1))).IsValid.Should().BeFalse();
        v.Validate(new BulkCancelSessionsCommand(Guid.CreateVersion7(), "x", d, d)).IsValid.Should().BeTrue();
    }

    // ── Toplu oda değişikliği ───────────────────────────────────────────────

    [Fact]
    public async Task BulkReassignRoom_moves_sessions_keeps_status_skips_conflicts_and_updates_the_plan()
    {
        using var ctx = new TenantTestContext();
        var oldRoom = Room.Create(TenantA, "A", "Eski", 4, false);
        var newRoom = Room.Create(TenantA, "B", "Yeni", 4, false);
        var plan = Plan(oldRoom.Id);
        var s1 = InPlan(plan, 0, oldRoom.Id);
        var s2 = InPlan(plan, 7, oldRoom.Id); // yeni odada çakışıyor
        var s3 = InPlan(plan, 14, oldRoom.Id);
        await ctx.SeedAsync(oldRoom, newRoom, plan, s1, s2, s3);

        var repo = new Mock<ISessionRepository>();
        repo.Setup(r => r.HasRoomConflictAsync(newRoom.Id, It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), s2.Id, default)).ReturnsAsync(true);
        var handler = new BulkReassignRoomCommandHandler(ctx.Db, repo.Object, ctx.CurrentUser.Object);

        var result = await handler.Handle(new BulkReassignRoomCommand(plan.Id, newRoom.Id, DateOnly.FromDateTime(Future.UtcDateTime)), default);

        result.AffectedCount.Should().Be(2);
        result.RoomConflictSkipped.Should().Be(1);
        var byId = await ctx.Db.Sessions.ToDictionaryAsync(s => s.Id);
        byId[s1.Id].RoomId.Should().Be(newRoom.Id);
        byId[s1.Id].Status.Should().Be("scheduled", "oda değişikliği seansı 'ertelendi' yapmaz");
        byId[s2.Id].RoomId.Should().Be(oldRoom.Id);
        byId[s3.Id].RoomId.Should().Be(newRoom.Id);
        (await ctx.Db.RecurringSchedules.SingleAsync()).RoomId.Should().Be(newRoom.Id);
    }

    [Fact]
    public async Task BulkReassignRoom_rejects_an_unknown_room()
    {
        using var ctx = new TenantTestContext();
        var plan = Plan();
        await ctx.SeedAsync(plan);
        var act = () => ctx.Handler<BulkReassignRoomCommandHandler>().Handle(
            new BulkReassignRoomCommand(plan.Id, Guid.CreateVersion7(), new DateOnly(2027, 1, 1)), default);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── Tek tarih istisnası ─────────────────────────────────────────────────

    [Fact]
    public async Task Exception_skip_cancels_the_session_already_generated_for_that_date()
    {
        using var ctx = new TenantTestContext();
        var plan = Plan();
        var onDate = InPlan(plan, 0);
        var other = InPlan(plan, 7);
        await ctx.SeedAsync(plan, onDate, other);

        var result = await ctx.Handler<AddRecurrenceExceptionCommandHandler>().Handle(
            new AddRecurrenceExceptionCommand(plan.Id, DateOnly.FromDateTime(Future.UtcDateTime), "skip", null, "Eğitmen izinli"), default);

        result.AffectedCount.Should().Be(1);
        var byId = await ctx.Db.Sessions.ToDictionaryAsync(s => s.Id);
        byId[onDate.Id].Status.Should().Be("cancelled");
        byId[onDate.Id].CancelReason.Should().Be("Eğitmen izinli");
        byId[other.Id].Status.Should().Be("scheduled");
        (await ctx.Db.RecurrenceExceptions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Exception_reschedule_moves_the_existing_session_keeping_its_duration()
    {
        using var ctx = new TenantTestContext();
        var plan = Plan();
        var onDate = InPlan(plan, 0);
        await ctx.SeedAsync(plan, onDate);
        var newStart = Future.AddHours(3);

        await ctx.Handler<AddRecurrenceExceptionCommandHandler>().Handle(
            new AddRecurrenceExceptionCommand(plan.Id, DateOnly.FromDateTime(Future.UtcDateTime), "reschedule", newStart, null), default);

        var moved = await ctx.Db.Sessions.SingleAsync();
        moved.StartsAt.Should().Be(newStart);
        moved.EndsAt.Should().Be(newStart.AddMinutes(40));
        moved.Status.Should().Be("rescheduled");
    }

    [Fact]
    public async Task Exception_for_the_same_date_twice_is_rejected_and_without_a_session_nothing_is_touched()
    {
        using var ctx = new TenantTestContext();
        var plan = Plan();
        await ctx.SeedAsync(plan);
        var date = DateOnly.FromDateTime(Future.UtcDateTime);
        var handler = ctx.Handler<AddRecurrenceExceptionCommandHandler>();

        var first = await handler.Handle(new AddRecurrenceExceptionCommand(plan.Id, date, "skip", null, null), default);
        first.AffectedCount.Should().Be(0);

        var act = () => handler.Handle(new AddRecurrenceExceptionCommand(plan.Id, date, "cancel", null, null), default);
        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }
}
