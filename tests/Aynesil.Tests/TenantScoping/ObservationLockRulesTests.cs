using Aynesil.Application.Features.Consultancy.Commands;
using Aynesil.Application.Features.Consultancy.Queries;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Consultancy.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Gözlem kilidi: tamamlanmış ziyarette düzenlenir ama silinmez (409); iptal edilmiş ziyarette ikisi de kapalı;
/// ziyarete rapor yazılmışsa düzenleme ve silme kapanır.
/// </summary>
public class ObservationLockRulesTests
{
    private static readonly DateOnly Day = new(2026, 12, 1);

    private static async Task ShouldBlockAsync(Func<Task> act, MsgKey key)
    {
        var ex = await Record.ExceptionAsync(act);
        ex.Should().BeOfType<BusinessRuleViolationException>();
        ((BusinessRuleViolationException)ex!).Code.Should().Be(key);
    }

    private static async Task<(TenantTestContext Ctx, SchoolVisit Visit, ObservationRecord Obs)> SeedAsync(string status, bool withReport = false)
    {
        var ctx = new TenantTestContext();
        var inst = Institution.Create(TenantA, "Okul");
        var visit = SchoolVisit.Schedule(TenantA, inst.Id, Day);
        if (status == "completed") visit.Complete();
        if (status == "cancelled") visit.Cancel();
        var obs = ObservationRecord.Record(TenantA, visit.Id, "gözlem");
        var seeds = new List<object> { inst, visit, obs };
        if (withReport) seeds.Add(ConsultancyReport.Create(TenantA, "Rapor", null, visit.Id, "özet"));
        await ctx.SeedAsync(seeds.ToArray());
        return (ctx, visit, obs);
    }

    private static Task Update(TenantTestContext c, ObservationRecord o) =>
        c.Handler<UpdateObservationCommandHandler>().Handle(
            new UpdateObservationCommand(o.Id, "düzenlendi", null, "konu", "öneri"), default);

    private static Task Delete(TenantTestContext c, ObservationRecord o) =>
        c.Handler<DeleteObservationCommandHandler>().Handle(new DeleteObservationCommand(o.Id), default);

    [Fact]
    public async Task Planned_visit_allows_edit_and_delete()
    {
        var (ctx, _, obs) = await SeedAsync("planned");
        using var _ = ctx;
        await Update(ctx, obs);
        (await ctx.Db.ObservationRecords.SingleAsync()).Observation.Should().Be("düzenlendi");
        await Delete(ctx, obs);
        (await ctx.Db.ObservationRecords.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Completed_visit_allows_edit_but_blocks_delete()
    {
        var (ctx, _, obs) = await SeedAsync("completed");
        using var _ = ctx;
        await Update(ctx, obs);
        (await ctx.Db.ObservationRecords.SingleAsync()).Observation.Should().Be("düzenlendi");
        await ShouldBlockAsync(() => Delete(ctx, obs), MsgKeys.Rule.ObservationDeleteNeedsPlannedVisit);
        (await ctx.Db.ObservationRecords.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Cancelled_visit_blocks_edit_and_delete()
    {
        var (ctx, _, obs) = await SeedAsync("cancelled");
        using var _ = ctx;
        await ShouldBlockAsync(() => Update(ctx, obs), MsgKeys.Rule.ObservationVisitCancelled);
        await ShouldBlockAsync(() => Delete(ctx, obs), MsgKeys.Rule.ObservationDeleteNeedsPlannedVisit);
    }

    [Theory]
    [InlineData("planned")]
    [InlineData("completed")]
    public async Task Visit_with_report_blocks_edit_and_delete(string status)
    {
        var (ctx, _, obs) = await SeedAsync(status, withReport: true);
        using var _ = ctx;
        await ShouldBlockAsync(() => Update(ctx, obs), MsgKeys.Rule.ObservationVisitHasReport);
        await ShouldBlockAsync(() => Delete(ctx, obs), MsgKeys.Rule.ObservationVisitHasReport);
        (await ctx.Db.ObservationRecords.SingleAsync()).Observation.Should().Be("gözlem");
    }

    [Fact]
    public async Task Visit_detail_reports_HasReports_flag()
    {
        var (without, v1, _) = await SeedAsync("completed");
        var (with, v2, _) = await SeedAsync("completed", withReport: true);
        using var _a = without; using var _b = with;

        (await without.Handler<GetSchoolVisitQueryHandler>().Handle(new GetSchoolVisitQuery(v1.Id), default)).HasReports.Should().BeFalse();
        (await with.Handler<GetSchoolVisitQueryHandler>().Handle(new GetSchoolVisitQuery(v2.Id), default)).HasReports.Should().BeTrue();
    }
}
