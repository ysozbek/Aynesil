using Aynesil.Application.Features.Consultancy.Commands;
using Aynesil.Domain.Modules.Consultancy.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

public class ConsultancyCommandTenantTests
{
    private static readonly DateOnly VisitDate = new(2026, 12, 1);

    private static (Institution Institution, ConsultancyPlan Plan) InstitutionWithPlan(Guid corp)
    {
        var institution = Institution.Create(corp, "Okul");
        return (institution, ConsultancyPlan.Create(corp, institution.Id, "Plan"));
    }

    // ── CreateInstitution (no parent) ─────────────────────────────────────────

    [Fact]
    public async Task CreateInstitution_uses_tenant_and_acting_user()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<CreateInstitutionCommandHandler>().Handle(
            new CreateInstitutionCommand("Okul", null, null, null, null, null, null), default);

        var saved = await ctx.Db.Institutions.SingleAsync();
        saved.CorporationId.Should().Be(TenantA);
        saved.CreatedBy.Should().Be(ctx.UserId);
    }

    // ── CreateConsultancyPlan ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateConsultancyPlan_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var institution = Institution.Create(TenantA, "Okul");
        await ctx.SeedAsync(institution);

        await ctx.Handler<CreateConsultancyPlanCommandHandler>().Handle(
            new CreateConsultancyPlanCommand(institution.Id, "Plan", null, null, null, null, null), default);

        (await ctx.Db.ConsultancyPlans.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateConsultancyPlan_rejects_institution_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var institution = Institution.Create(TenantB, "Okul");
        await ctx.SeedAsync(institution);

        await ShouldRejectAsync(() => ctx.Handler<CreateConsultancyPlanCommandHandler>().Handle(
            new CreateConsultancyPlanCommand(institution.Id, "Plan", null, null, null, null, null), default));
        (await ctx.Db.ConsultancyPlans.CountAsync()).Should().Be(0);
    }

    // ── CreateConsultancyAgreement ────────────────────────────────────────────

    [Fact]
    public async Task CreateConsultancyAgreement_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var (institution, plan) = InstitutionWithPlan(TenantA);
        await ctx.SeedAsync(institution, plan);

        await ctx.Handler<CreateConsultancyAgreementCommandHandler>().Handle(
            new CreateConsultancyAgreementCommand(plan.Id, "Sözleşme", null, null, null, null), default);

        (await ctx.Db.ConsultancyAgreements.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateConsultancyAgreement_rejects_plan_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var (institution, plan) = InstitutionWithPlan(TenantB);
        await ctx.SeedAsync(institution, plan);

        await ShouldRejectAsync(() => ctx.Handler<CreateConsultancyAgreementCommandHandler>().Handle(
            new CreateConsultancyAgreementCommand(plan.Id, "Sözleşme", null, null, null, null), default));
        (await ctx.Db.ConsultancyAgreements.CountAsync()).Should().Be(0);
    }

    // ── ScheduleSchoolVisit ───────────────────────────────────────────────────

    [Fact]
    public async Task ScheduleSchoolVisit_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var institution = Institution.Create(TenantA, "Okul");
        await ctx.SeedAsync(institution);

        await ctx.Handler<ScheduleSchoolVisitCommandHandler>().Handle(
            new ScheduleSchoolVisitCommand(institution.Id, VisitDate, null, null, null), default);

        (await ctx.Db.SchoolVisits.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task ScheduleSchoolVisit_rejects_institution_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var institution = Institution.Create(TenantB, "Okul");
        await ctx.SeedAsync(institution);

        await ShouldRejectAsync(() => ctx.Handler<ScheduleSchoolVisitCommandHandler>().Handle(
            new ScheduleSchoolVisitCommand(institution.Id, VisitDate, null, null, null), default));
        (await ctx.Db.SchoolVisits.CountAsync()).Should().Be(0);
    }

    // ── CreateConsultancyReport ───────────────────────────────────────────────

    [Fact]
    public async Task CreateConsultancyReport_uses_tenant_and_acting_user()
    {
        using var ctx = new TenantTestContext();
        var (institution, plan) = InstitutionWithPlan(TenantA);
        await ctx.SeedAsync(institution, plan);

        await ctx.Handler<CreateConsultancyReportCommandHandler>().Handle(
            new CreateConsultancyReportCommand("Rapor", plan.Id, null, "Özet", null), default);

        var saved = await ctx.Db.ConsultancyReports.SingleAsync();
        saved.CorporationId.Should().Be(TenantA);
        saved.AuthoredBy.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task CreateConsultancyReport_rejects_plan_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var (institution, plan) = InstitutionWithPlan(TenantB);
        await ctx.SeedAsync(institution, plan);

        await ShouldRejectAsync(() => ctx.Handler<CreateConsultancyReportCommandHandler>().Handle(
            new CreateConsultancyReportCommand("Rapor", plan.Id, null, "Özet", null), default));
        (await ctx.Db.ConsultancyReports.CountAsync()).Should().Be(0);
    }

    // ── CreateFollowUpActivity ────────────────────────────────────────────────

    [Fact]
    public async Task CreateFollowUpActivity_uses_tenant_and_acting_user()
    {
        using var ctx = new TenantTestContext();
        var (institution, plan) = InstitutionWithPlan(TenantA);
        await ctx.SeedAsync(institution, plan);

        await ctx.Handler<CreateFollowUpActivityCommandHandler>().Handle(
            new CreateFollowUpActivityCommand("Takip", plan.Id, null, null, null, null, null), default);

        var saved = await ctx.Db.FollowUpActivities.SingleAsync();
        saved.CorporationId.Should().Be(TenantA);
        saved.CreatedBy.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task CreateFollowUpActivity_rejects_plan_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var (institution, plan) = InstitutionWithPlan(TenantB);
        await ctx.SeedAsync(institution, plan);

        await ShouldRejectAsync(() => ctx.Handler<CreateFollowUpActivityCommandHandler>().Handle(
            new CreateFollowUpActivityCommand("Takip", plan.Id, null, null, null, null, null), default));
        (await ctx.Db.FollowUpActivities.CountAsync()).Should().Be(0);
    }
}
