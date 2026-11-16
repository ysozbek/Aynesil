using Aynesil.Application.Features.Consultancy.Commands;
using Aynesil.Domain.Common;
using Aynesil.Domain.Modules.Consultancy.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Danışmanlık silmelerinde bağlı kayıt varsa ham FK hatası ya da sessiz cascade yerine
/// anlamlı bir iş kuralı hatası dönmeli; ziyaret güncellemesi plan kuralını Schedule ile paylaşmalı.
/// </summary>
public class ConsultancyDeleteRulesTests
{
    private static readonly DateOnly Day = new(2026, 12, 1);

    private static async Task ShouldBlockAsync(Func<Task> act, MsgKey key)
    {
        var ex = await Record.ExceptionAsync(act);
        ex.Should().BeOfType<BusinessRuleViolationException>();
        ((BusinessRuleViolationException)ex!).Code.Should().Be(key);
    }

    private static ConsultancyPlan ActivePlan(Guid corp, Guid institutionId)
    {
        var plan = ConsultancyPlan.Create(corp, institutionId, "Plan");
        plan.Activate();
        return plan;
    }

    // ── Plan ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeletePlan_with_agreement_is_blocked_even_when_agreement_is_soft_deleted()
    {
        using var ctx = new TenantTestContext();
        var inst = Institution.Create(TenantA, "Okul");
        var plan = ConsultancyPlan.Create(TenantA, inst.Id, "Plan");
        var agreement = ConsultancyAgreement.Create(TenantA, plan.Id, inst.Id, "Sözleşme");
        agreement.SoftDelete(ctx.UserId);
        await ctx.SeedAsync(inst, plan, agreement);

        await ShouldBlockAsync(
            () => ctx.Handler<DeleteConsultancyPlanCommandHandler>().Handle(new DeleteConsultancyPlanCommand(plan.Id), default),
            MsgKeys.Rule.PlanHasAgreements);
    }

    [Fact]
    public async Task DeletePlan_with_report_or_follow_up_is_blocked()
    {
        using var ctx = new TenantTestContext();
        var inst = Institution.Create(TenantA, "Okul");
        var withReport = ConsultancyPlan.Create(TenantA, inst.Id, "Raporlu");
        var withFollowUp = ConsultancyPlan.Create(TenantA, inst.Id, "Takipli");
        await ctx.SeedAsync(inst, withReport, withFollowUp,
            ConsultancyReport.Create(TenantA, "Rapor", withReport.Id, summary: "özet"),
            FollowUpActivity.Create(TenantA, "Takip", withFollowUp.Id, null));

        await ShouldBlockAsync(
            () => ctx.Handler<DeleteConsultancyPlanCommandHandler>().Handle(new DeleteConsultancyPlanCommand(withReport.Id), default),
            MsgKeys.Rule.PlanHasReports);
        await ShouldBlockAsync(
            () => ctx.Handler<DeleteConsultancyPlanCommandHandler>().Handle(new DeleteConsultancyPlanCommand(withFollowUp.Id), default),
            MsgKeys.Rule.PlanHasFollowUps);
    }

    [Fact]
    public async Task DeletePlan_without_dependents_succeeds()
    {
        using var ctx = new TenantTestContext();
        var inst = Institution.Create(TenantA, "Okul");
        var plan = ConsultancyPlan.Create(TenantA, inst.Id, "Plan");
        await ctx.SeedAsync(inst, plan);

        await ctx.Handler<DeleteConsultancyPlanCommandHandler>().Handle(new DeleteConsultancyPlanCommand(plan.Id), default);

        (await ctx.Db.ConsultancyPlans.CountAsync()).Should().Be(0);
    }

    // ── Visit ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteVisit_with_report_or_follow_up_is_blocked()
    {
        using var ctx = new TenantTestContext();
        var inst = Institution.Create(TenantA, "Okul");
        var withReport = SchoolVisit.Schedule(TenantA, inst.Id, Day);
        var withFollowUp = SchoolVisit.Schedule(TenantA, inst.Id, Day);
        await ctx.SeedAsync(inst, withReport, withFollowUp,
            ConsultancyReport.Create(TenantA, "Rapor", null, withReport.Id, "özet"),
            FollowUpActivity.Create(TenantA, "Takip", null, withFollowUp.Id));

        await ShouldBlockAsync(
            () => ctx.Handler<DeleteSchoolVisitCommandHandler>().Handle(new DeleteSchoolVisitCommand(withReport.Id), default),
            MsgKeys.Rule.VisitHasReports);
        await ShouldBlockAsync(
            () => ctx.Handler<DeleteSchoolVisitCommandHandler>().Handle(new DeleteSchoolVisitCommand(withFollowUp.Id), default),
            MsgKeys.Rule.VisitHasFollowUps);
    }

    [Fact]
    public async Task DeleteVisit_with_observation_is_blocked()
    {
        using var ctx = new TenantTestContext();
        var inst = Institution.Create(TenantA, "Okul");
        var visit = SchoolVisit.Schedule(TenantA, inst.Id, Day);
        await ctx.SeedAsync(inst, visit, ObservationRecord.Record(TenantA, visit.Id, "gözlem"));

        await ShouldBlockAsync(
            () => ctx.Handler<DeleteSchoolVisitCommandHandler>().Handle(new DeleteSchoolVisitCommand(visit.Id), default),
            MsgKeys.Rule.VisitHasObservations);
    }

    [Fact]
    public async Task UpdateVisit_rejects_plan_of_other_institution_and_accepts_active_plan_of_same_institution()
    {
        using var ctx = new TenantTestContext();
        var inst = Institution.Create(TenantA, "Okul");
        var other = Institution.Create(TenantA, "Başka Okul");
        var okPlan = ActivePlan(TenantA, inst.Id);
        var foreignPlan = ActivePlan(TenantA, other.Id);
        var visit = SchoolVisit.Schedule(TenantA, inst.Id, Day);
        await ctx.SeedAsync(inst, other, okPlan, foreignPlan, visit);

        await ShouldBlockAsync(
            () => ctx.Handler<UpdateSchoolVisitCommandHandler>().Handle(
                new UpdateSchoolVisitCommand(visit.Id, Day, null, null, foreignPlan.Id), default),
            MsgKeys.Rule.ConsultancyPlanNotActive);

        await ctx.Handler<UpdateSchoolVisitCommandHandler>().Handle(
            new UpdateSchoolVisitCommand(visit.Id, Day.AddDays(1), null, "amaç", okPlan.Id), default);
        var saved = await ctx.Db.SchoolVisits.SingleAsync();
        saved.ConsultancyPlanId.Should().Be(okPlan.Id);
        saved.VisitDate.Should().Be(Day.AddDays(1));
    }

    [Fact]
    public async Task UpdateVisit_of_another_tenant_is_not_found()
    {
        using var ctx = new TenantTestContext();
        var inst = Institution.Create(TenantB, "Okul");
        var visit = SchoolVisit.Schedule(TenantB, inst.Id, Day);
        await ctx.SeedAsync(inst, visit);

        await ShouldRejectAsync(() => ctx.Handler<UpdateSchoolVisitCommandHandler>().Handle(
            new UpdateSchoolVisitCommand(visit.Id, Day, null, null, null), default));
    }

    // ── Institution ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("plan")]
    [InlineData("visit")]
    [InlineData("agreement")]
    public async Task DeleteInstitution_with_any_related_record_is_blocked(string kind)
    {
        using var ctx = new TenantTestContext();
        var inst = Institution.Create(TenantA, "Okul");
        var plan = ConsultancyPlan.Create(TenantA, inst.Id, "Plan");
        var seeds = new List<object> { inst };
        switch (kind)
        {
            case "plan": seeds.Add(plan); break;
            case "visit": seeds.Add(SchoolVisit.Schedule(TenantA, inst.Id, Day)); break;
            default: seeds.Add(plan); seeds.Add(ConsultancyAgreement.Create(TenantA, plan.Id, inst.Id, "S")); break;
        }
        await ctx.SeedAsync(seeds.ToArray());

        await ShouldBlockAsync(
            () => ctx.Handler<DeleteInstitutionCommandHandler>().Handle(new DeleteInstitutionCommand(inst.Id), default),
            MsgKeys.Rule.InstitutionHasRecords);
    }

    [Fact]
    public async Task DeleteInstitution_without_records_is_soft_deleted()
    {
        using var ctx = new TenantTestContext();
        var inst = Institution.Create(TenantA, "Okul");
        await ctx.SeedAsync(inst);

        await ctx.Handler<DeleteInstitutionCommandHandler>().Handle(new DeleteInstitutionCommand(inst.Id), default);

        (await ctx.Db.Institutions.CountAsync()).Should().Be(0);
    }
}
