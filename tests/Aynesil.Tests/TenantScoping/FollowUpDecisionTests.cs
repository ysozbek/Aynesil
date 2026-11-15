using Aynesil.Application.Common.Exceptions;
using Aynesil.Application.Features.Finance.Commands;
using Aynesil.Application.Features.Meetings.Commands;
using Aynesil.Application.Features.Scheduling.Commands;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Finance.Entities;
using Aynesil.Domain.Modules.Ops.Entities;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>Karar turu: tamamlanmış toplantıya sonuç, indirim kaldırma, sabit onaylayan, kapalı gün → seans iptali.</summary>
public class FollowUpDecisionTests
{
    // ── Toplantı sonucu ─────────────────────────────────────────────────────

    [Fact]
    public async Task Outcome_can_be_added_and_edited_on_a_completed_meeting()
    {
        using var ctx = new TenantTestContext();
        var meeting = Meeting.Create(TenantA, "Değerlendirme");
        meeting.Complete();
        await ctx.SeedAsync(meeting);

        var dto = await ctx.Handler<RecordMeetingOutcomeCommandHandler>().Handle(
            new RecordMeetingOutcomeCommand(meeting.Id, "Özet", "Karar"), default);
        await ctx.Handler<UpdateMeetingOutcomeCommandHandler>().Handle(
            new UpdateMeetingOutcomeCommand(dto.Id, "Yeni özet", null), default);

        (await ctx.Db.MeetingOutcomes.SingleAsync()).Summary.Should().Be("Yeni özet");
    }

    [Fact]
    public async Task Outcome_is_rejected_on_a_cancelled_meeting_for_add_and_edit()
    {
        using var ctx = new TenantTestContext();
        var meeting = Meeting.Create(TenantA, "İptal");
        var outcome = MeetingOutcome.Create(TenantA, meeting.Id, "Eski", null);
        meeting.Cancel();
        await ctx.SeedAsync(meeting, outcome);

        var add = () => ctx.Handler<RecordMeetingOutcomeCommandHandler>().Handle(
            new RecordMeetingOutcomeCommand(meeting.Id, "x", null), default);
        var edit = () => ctx.Handler<UpdateMeetingOutcomeCommandHandler>().Handle(
            new UpdateMeetingOutcomeCommand(outcome.Id, "y", null), default);

        await add.Should().ThrowAsync<BusinessRuleViolationException>();
        await edit.Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.MeetingOutcomes.SingleAsync()).Summary.Should().Be("Eski");
    }

    // ── İndirim kaldırma ────────────────────────────────────────────────────

    private static async Task<(TenantTestContext ctx, Invoice invoice, Guid discountId)> DraftWithDiscount()
    {
        var ctx = new TenantTestContext();
        var invoice = Invoice.Create(TenantA, new DateOnly(2026, 11, 15));
        invoice.Lines.Add(InvoiceLine.Create(TenantA, invoice.Id, "Paket", 1000m));
        invoice.RecalculateTotals();
        await ctx.SeedAsync(invoice);
        var d = await ctx.Handler<ApplyDiscountCommandHandler>().Handle(new ApplyDiscountCommand(10m, true, invoice.Id, Reason: "x"), default);
        ctx.Db.ChangeTracker.Clear();
        return (ctx, invoice, d.Id);
    }

    [Fact]
    public async Task Removing_a_discount_from_a_draft_restores_the_totals()
    {
        var (ctx, invoice, discountId) = await DraftWithDiscount();
        using var _ = ctx;

        await ctx.Handler<RemoveDiscountCommandHandler>().Handle(new RemoveDiscountCommand(discountId), default);

        ctx.Db.ChangeTracker.Clear();
        (await ctx.Db.Discounts.CountAsync()).Should().Be(0);
        var saved = await ctx.Db.Invoices.SingleAsync();
        saved.DiscountTotal.Should().Be(0m);
        saved.GrandTotal.Should().Be(1000m);
    }

    [Fact]
    public async Task Removing_a_discount_from_an_issued_invoice_is_rejected_and_unknown_is_not_found()
    {
        var (ctx, invoice, discountId) = await DraftWithDiscount();
        using var _ = ctx;
        var tracked = await ctx.Db.Invoices.SingleAsync();
        tracked.Issue();
        await ctx.Db.SaveChangesAsync();
        ctx.Db.ChangeTracker.Clear();

        var act = () => ctx.Handler<RemoveDiscountCommandHandler>().Handle(new RemoveDiscountCommand(discountId), default);
        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        (await ctx.Db.Discounts.CountAsync()).Should().Be(1);

        var missing = () => ctx.Handler<RemoveDiscountCommandHandler>().Handle(new RemoveDiscountCommand(Guid.CreateVersion7()), default);
        await missing.Should().ThrowAsync<NotFoundException>();
    }

    // ── Burs onaylayanı ─────────────────────────────────────────────────────

    [Fact]
    public async Task Scholarship_edit_keeps_the_original_approver()
    {
        using var ctx = new TenantTestContext();
        var approver = Guid.CreateVersion7();
        var sch = Scholarship.Grant(TenantA, Guid.CreateVersion7(), percentage: 10m, approvedBy: approver);
        await ctx.SeedAsync(sch);

        var dto = await ctx.Handler<UpdateScholarshipCommandHandler>().Handle(
            new UpdateScholarshipCommand(sch.Id, null, 20m, null, null, null, "Not", sch.RowVersion), default);

        dto.ApprovedBy.Should().Be(approver).And.NotBe(ctx.UserId);
    }

    // ── Kapalı gün → planlı seansları iptal et ──────────────────────────────

    private static readonly DateTimeOffset Day = new(2026, 12, 30, 0, 0, 0, TimeSpan.Zero);

    private static Session At(int hour, Guid? campus = null) =>
        Session.Schedule(TenantA, Guid.CreateVersion7(), Day.AddHours(hour), Day.AddHours(hour).AddMinutes(40), campusId: campus);

    [Fact]
    public async Task Impact_counts_only_planned_sessions_in_range_and_campus()
    {
        using var ctx = new TenantTestContext();
        var campus = Guid.CreateVersion7();
        var done = At(9); done.Complete();
        await ctx.SeedAsync(At(10), At(11, campus), At(12, Guid.CreateVersion7()), done,
            Session.Schedule(TenantA, Guid.CreateVersion7(), Day.AddDays(2), Day.AddDays(2).AddMinutes(40)));

        var all = await ctx.Handler<GetCalendarEntryImpactQueryHandler>().Handle(
            new GetCalendarEntryImpactQuery(Day, Day.AddDays(1), null), default);
        var one = await ctx.Handler<GetCalendarEntryImpactQueryHandler>().Handle(
            new GetCalendarEntryImpactQuery(Day, Day.AddDays(1), campus), default);

        all.PlannedSessionCount.Should().Be(3);
        one.PlannedSessionCount.Should().Be(1);
    }

    [Fact]
    public async Task Creating_a_closure_with_the_option_cancels_planned_sessions_using_the_title_as_reason()
    {
        using var ctx = new TenantTestContext();
        var inside = At(10); var outside = Session.Schedule(TenantA, Guid.CreateVersion7(), Day.AddDays(3), Day.AddDays(3).AddMinutes(40));
        var done = At(9); done.Complete();
        await ctx.SeedAsync(inside, outside, done);

        var dto = await ctx.Handler<CreateCalendarEntryCommandHandler>().Handle(
            new CreateCalendarEntryCommand("Yıl sonu bakım", "closure", Day, Day.AddDays(1), true, null, true), default);

        dto.CancelledSessionCount.Should().Be(1);
        var byId = await ctx.Db.Sessions.ToDictionaryAsync(s => s.Id);
        byId[inside.Id].Status.Should().Be("cancelled");
        byId[inside.Id].CancelReason.Should().Be("Yıl sonu bakım");
        byId[outside.Id].Status.Should().Be("scheduled");
        byId[done.Id].Status.Should().Be("completed");
    }

    [Fact]
    public async Task Without_the_option_or_for_an_event_sessions_are_left_alone()
    {
        using var ctx = new TenantTestContext();
        var s = At(10);
        await ctx.SeedAsync(s);
        var handler = ctx.Handler<CreateCalendarEntryCommandHandler>();

        var plain = await handler.Handle(new CreateCalendarEntryCommand("Tatil", "holiday", Day, Day.AddDays(1), true, null), default);
        var ev = await handler.Handle(new CreateCalendarEntryCommand("Gezi", "event", Day, Day.AddDays(1), true, null, true), default);

        plain.CancelledSessionCount.Should().Be(0);
        ev.CancelledSessionCount.Should().Be(0);
        (await ctx.Db.Sessions.SingleAsync()).Status.Should().Be("scheduled");
    }
}
