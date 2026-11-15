using Aynesil.Application.Features.Scheduling.Commands;
using Aynesil.Domain.Modules.Scheduling.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Tekrar eden plandan seans üretilirken tatil / kapalı gün / dönem arası kayıtları atlanır
/// ve atlanan tarih sayısı sonuçta döner; "etkinlik" kaydı ve başka kampüsün kaydı engellemez.
/// </summary>
public class RecurringClosureSkipTests
{
    private static DateTimeOffset Day(int d, int h = 0) => new(2026, 12, d, h, 0, 0, TimeSpan.Zero);

    private static RecurringSchedule Weekly(Guid? campusId = null) => RecurringSchedule.Create(
        TenantA, "weekly", new TimeOnly(10, 0), 40, new DateOnly(2026, 12, 1), campusId,
        null, Guid.CreateVersion7(), null, 1, [1], null, null, null, Guid.CreateVersion7());

    private static CalendarEntry Entry(string type, int day, Guid? campusId = null)
        => CalendarEntry.Create(TenantA, "Kayıt", type, Day(day), Day(day + 1), true, campusId);

    [Fact]
    public async Task Holiday_and_closure_dates_are_skipped_and_counted()
    {
        using var ctx = new TenantTestContext();
        var schedule = Weekly();
        var otherCampus = Guid.CreateVersion7();
        await ctx.SeedAsync(
            schedule,
            Entry("holiday", 14),                // pazartesi → atlanır
            Entry("event", 21),                  // etkinlik engellemez
            Entry("closure", 28, otherCampus));  // plan kampüssüz değil: kapalı kayıt başka kampüse ait, ama plan kampüssüz

        var result = await ctx.Handler<BulkGenerateSessionsCommandHandler>().Handle(
            new BulkGenerateSessionsCommand(schedule.Id, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 28)), default);

        // Pazartesiler: 7, 14, 21, 28. Plan kampüssüz (null) → yalnızca kurum geneli kayıtlar ve
        // kampüssüz plana "== null" ile eşleşenler geçerli; kampüse özel kayıt (28) planı etkilemez.
        result.ClosureSkipped.Should().Be(1);
        result.AffectedCount.Should().Be(3);
        var starts = await ctx.Db.Sessions.Select(s => s.StartsAt.Day).ToListAsync();
        starts.Should().BeEquivalentTo([7, 21, 28]);
    }

    [Fact]
    public async Task Campus_closure_applies_only_to_plans_of_that_campus()
    {
        using var ctx = new TenantTestContext();
        var campus = Guid.CreateVersion7();
        var schedule = Weekly(campus);
        await ctx.SeedAsync(schedule, Entry("term_break", 7, campus), Entry("closure", 14, Guid.CreateVersion7()));

        var result = await ctx.Handler<BulkGenerateSessionsCommandHandler>().Handle(
            new BulkGenerateSessionsCommand(schedule.Id, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 14)), default);

        result.ClosureSkipped.Should().Be(1);
        result.AffectedCount.Should().Be(1); // yalnızca 14'ü (başka kampüsün kapanışı etkilemez)
    }

    [Fact]
    public async Task Without_closures_nothing_is_skipped()
    {
        using var ctx = new TenantTestContext();
        var schedule = Weekly();
        await ctx.SeedAsync(schedule);

        var result = await ctx.Handler<BulkGenerateSessionsCommandHandler>().Handle(
            new BulkGenerateSessionsCommand(schedule.Id, new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 14)), default);

        result.ClosureSkipped.Should().Be(0);
        result.AffectedCount.Should().Be(2);
    }
}
