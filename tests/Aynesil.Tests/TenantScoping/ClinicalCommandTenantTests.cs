using Aynesil.Application.Features.Assessment.Commands;
using Aynesil.Application.Features.Goals.Commands;
using Aynesil.Application.Features.Legal.Commands;
using Aynesil.Application.Features.Plans.Commands;
using Aynesil.Domain.Modules.Assessment.Entities;
using Aynesil.Domain.Modules.Education.Entities;
using Aynesil.Domain.Modules.Legal.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>Goals, education plans (BEP), assessment and legal (consents / contracts).</summary>
public class ClinicalCommandTenantTests
{
    private static Student NewStudent(Guid corp) => Student.Create(corp, "Ali", "Kaya");

    // ── Goals ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateGoalLibrary_creates_a_tenant_library_never_a_global_one()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<CreateGoalLibraryCommandHandler>().Handle(new CreateGoalLibraryCommand("Dil", null), default);
        (await ctx.Db.GoalLibraries.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateGoalTemplate_creates_a_tenant_template_never_a_global_one()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<CreateGoalTemplateCommandHandler>().Handle(
            new CreateGoalTemplateCommand(null, null, null, null, "Hedef ifadesi", null), default);
        (await ctx.Db.GoalTemplates.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateStudentGoal_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantA);
        await ctx.SeedAsync(student);

        await ctx.Handler<CreateStudentGoalCommandHandler>().Handle(GoalCommand(student.Id), default);

        (await ctx.Db.StudentGoals.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateStudentGoal_rejects_template_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantA);
        var foreignTemplate = GoalTemplate.Create(TenantB, "Yabancı şablon");
        await ctx.SeedAsync(student, foreignTemplate);

        await ShouldRejectAsync(() => ctx.Handler<CreateStudentGoalCommandHandler>().Handle(
            GoalCommand(student.Id) with { TemplateId = foreignTemplate.Id }, default));
        (await ctx.Db.StudentGoals.CountAsync()).Should().Be(0);
    }

    private static CreateStudentGoalCommand GoalCommand(Guid studentId) =>
        new(studentId, "Göz teması kurar", "short_term", null, null, null, null, null, null, null, null, null);

    // ── Plans (BEP) ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAcademicPeriod_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<CreateAcademicPeriodCommandHandler>().Handle(
            new CreateAcademicPeriodCommand("2026-2027", new(2026, 9, 1), new(2027, 6, 30), null, false), default);
        (await ctx.Db.AcademicPeriods.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateEducationPlan_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantA);
        await ctx.SeedAsync(student);

        await ctx.Handler<CreateEducationPlanCommandHandler>().Handle(
            new CreateEducationPlanCommand(student.Id, "BEP", null, null, null, null, null), default);

        (await ctx.Db.EducationPlans.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateEducationPlan_rejects_student_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantB);
        await ctx.SeedAsync(student);

        await ShouldRejectAsync(() => ctx.Handler<CreateEducationPlanCommandHandler>().Handle(
            new CreateEducationPlanCommand(student.Id, "BEP", null, null, null, null, null), default));
        (await ctx.Db.EducationPlans.CountAsync()).Should().Be(0);
    }

    // ── Assessment ────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAssessmentTemplate_creates_a_tenant_template_never_a_global_one()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<CreateAssessmentTemplateCommandHandler>().Handle(
            new CreateAssessmentTemplateCommand("T1", "Test", null, null, null, null), default);
        (await ctx.Db.AssessmentTemplates.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateAssessmentSession_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var template = AssessmentTemplate.Create("T1", "Test", TenantA);
        await ctx.SeedAsync(template);

        await ctx.Handler<CreateAssessmentSessionCommandHandler>().Handle(
            new CreateAssessmentSessionCommand(template.Id, null, Guid.CreateVersion7(), null, null, null), default);

        (await ctx.Db.AssessmentSessions.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateAssessmentSession_rejects_template_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var template = AssessmentTemplate.Create("T1", "Test", TenantB);
        await ctx.SeedAsync(template);

        await ShouldRejectAsync(() => ctx.Handler<CreateAssessmentSessionCommandHandler>().Handle(
            new CreateAssessmentSessionCommand(template.Id, null, Guid.CreateVersion7(), null, null, null), default));
        (await ctx.Db.AssessmentSessions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateAssessmentReport_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var session = CompletedSession(TenantA);
        await ctx.SeedAsync(session);

        await ctx.Handler<CreateAssessmentReportCommandHandler>().Handle(
            new CreateAssessmentReportCommand(session.Id, "Özet", null, null), default);

        (await ctx.Db.AssessmentReports.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateAssessmentReport_rejects_session_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var session = CompletedSession(TenantB);
        await ctx.SeedAsync(session);

        await ShouldRejectAsync(() => ctx.Handler<CreateAssessmentReportCommandHandler>().Handle(
            new CreateAssessmentReportCommand(session.Id, "Özet", null, null), default));
        (await ctx.Db.AssessmentReports.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateProgramRecommendation_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<CreateProgramRecommendationCommandHandler>().Handle(
            new CreateProgramRecommendationCommand(null, null, Guid.CreateVersion7(), null, null, "Gerekçe", null), default);
        (await ctx.Db.ProgramRecommendations.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    private static AssessmentSession CompletedSession(Guid corp)
    {
        var session = AssessmentSession.Create(corp, Guid.CreateVersion7(), 1, null, Guid.CreateVersion7());
        session.Start();
        session.Complete(10m);
        return session;
    }

    // ── Legal ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateConsentTemplate_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<CreateConsentTemplateCommandHandler>().Handle(
            new CreateConsentTemplateCommand("KVKK", null, true, null, [new ConsentTranslationRequest("tr", "KVKK", "Metin")]), default);
        (await ctx.Db.ConsentTemplates.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task CreateContractTemplate_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        await ctx.Handler<CreateContractTemplateCommandHandler>().Handle(
            new CreateContractTemplateCommand("HIZMET", null, null, [new TemplateTranslationRequest("tr", "Hizmet", "Metin")]), default);
        (await ctx.Db.ContractTemplates.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task GrantConsent_uses_tenant_and_acting_user()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantA);
        await ctx.SeedAsync(student);

        await ctx.Handler<GrantConsentCommandHandler>().Handle(
            new GrantConsentCommand(student.Id, null, null, null, null, null), default);

        var saved = await ctx.Db.StudentConsents.SingleAsync();
        saved.CorporationId.Should().Be(TenantA);
        saved.CreatedBy.Should().Be(ctx.UserId);
    }

    [Fact]
    public async Task GrantConsent_rejects_student_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantB);
        await ctx.SeedAsync(student);

        await ShouldRejectAsync(() => ctx.Handler<GrantConsentCommandHandler>().Handle(
            new GrantConsentCommand(student.Id, null, null, null, null, null), default));
        (await ctx.Db.StudentConsents.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task GenerateStudentContract_uses_corporation_from_tenant_context()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantA);
        await ctx.SeedAsync(student);

        await ctx.Handler<GenerateStudentContractCommandHandler>().Handle(
            new GenerateStudentContractCommand(student.Id, null, null, null, null), default);

        (await ctx.Db.StudentContracts.SingleAsync()).CorporationId.Should().Be(TenantA);
    }

    [Fact]
    public async Task GenerateStudentContract_rejects_template_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var student = NewStudent(TenantA);
        var foreignTemplate = ContractTemplate.Create(TenantB, "HIZMET");
        await ctx.SeedAsync(student, foreignTemplate);

        await ShouldRejectAsync(() => ctx.Handler<GenerateStudentContractCommandHandler>().Handle(
            new GenerateStudentContractCommand(student.Id, foreignTemplate.Id, null, null, null), default));
        (await ctx.Db.StudentContracts.CountAsync()).Should().Be(0);
    }
}
