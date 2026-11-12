using System.Collections;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Assessment.Entities;
using Aynesil.Domain.Modules.Camps.Entities;
using Aynesil.Domain.Modules.Consultancy.Entities;
using Aynesil.Domain.Modules.Core.Entities;
using Aynesil.Domain.Modules.Crm.Entities;
using Aynesil.Domain.Modules.Education.Entities;
using Aynesil.Domain.Modules.Educators.Entities;
using Aynesil.Domain.Modules.Finance.Entities;
using Aynesil.Domain.Modules.Legal.Entities;
using Aynesil.Domain.Modules.Media.Entities;
using Aynesil.Domain.Modules.Ops.Entities;
using Aynesil.Domain.Modules.Scheduling.Entities;
using Aynesil.Domain.Modules.Students.Entities;
using FluentAssertions;
using MediatR;
using Assess = Aynesil.Application.Features.Assessment.Queries;
using Cams = Aynesil.Application.Features.Cameras.Queries;
using CampQ = Aynesil.Application.Features.Camps.Queries;
using Cmp = Aynesil.Application.Features.Campuses.Queries;
using Cons = Aynesil.Application.Features.Consultancy.Queries;
using Edu = Aynesil.Application.Features.Educators.Queries;
using Fin = Aynesil.Application.Features.Finance.Queries;
using Goal = Aynesil.Application.Features.Goals.Queries;
using LeadQ = Aynesil.Application.Features.Leads.Queries;
using Leave = Aynesil.Application.Features.Leaves.Queries;
using Legal = Aynesil.Application.Features.Legal.Queries;
using Meet = Aynesil.Application.Features.Meetings.Queries;
using Notif = Aynesil.Application.Features.Notifications.Queries;
using Kpi = Aynesil.Application.Features.PerformanceKpi.Queries;
using Plan = Aynesil.Application.Features.Plans.Queries;
using Prog = Aynesil.Application.Features.Programs.Queries;
using Sched = Aynesil.Application.Features.Scheduling.Queries;
using Stu = Aynesil.Application.Features.Students.Queries;
using Surv = Aynesil.Application.Features.Surveys.Queries;
using static Aynesil.Tests.TenantScoping.TenantTestContext;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Query handlers no longer accept a client CorporationId filter; they always scope to the tenant.
/// Each case seeds the same row for tenant A and tenant B (with the parent rows a list projection
/// joins to) and asserts the query, run as tenant A, returns only A's row. A parent id filter that
/// points at B's records therefore also yields nothing.
/// </summary>
public class QueryTenantIsolationTests
{
    /// <param name="Seed">Returns the rows for one tenant; the first row is the one the query must list.</param>
    public sealed record Case(string Name, Func<Guid, object[]> Seed, Func<object> Query)
    {
        public override string ToString() => Name;
    }

    private static readonly DateOnly D = new(2026, 11, 1);
    private static readonly DateTimeOffset T = new(2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

    private static Student St(Guid c) => Student.Create(c, "Ali", "Kaya");
    private static Institution Inst(Guid c) => Institution.Create(c, "Okul");

    private static object[] WithStudent(Guid c, Func<Guid, object> make) { var s = St(c); return [make(s.Id), s]; }
    private static object[] WithInstitution(Guid c, Func<Guid, object> make) { var i = Inst(c); return [make(i.Id), i]; }
    private static object[] WithPlan(Guid c, Func<Guid, Guid, object> make)
    {
        var i = Inst(c); var p = ConsultancyPlan.Create(c, i.Id, "Plan");
        return [make(p.Id, i.Id), p, i];
    }

    public static TheoryData<Case> Cases() =>
    [
        new("Students", c => [St(c)], () => new Stu.GetStudentsQuery()),
        new("Guardians", c => [Guardian.Create(c, "Ayşe", "Kaya")], () => new Stu.GetGuardiansQuery()),
        new("Educators", c => [Educator.Create(c, "Zeynep", "Ak")], () => new Edu.GetEducatorsQuery()),
        new("Campuses", c => [Campus.Create(c, "MRK", "Merkez"), NewCorporation(c)], () => new Cmp.GetCampusesQuery()),
        new("Cameras", c => [Camera.Register(c, "CAM", "Kamera")], () => new Cams.GetCamerasQuery()),
        new("ViewingAuthorizations", c => [ViewingAuthorization.Grant(c, Guid.CreateVersion7(), Guid.CreateVersion7(), T, T.AddDays(1))],
            () => new Cams.GetViewingAuthorizationsQuery()),
        new("ViewingLogs", c => [ViewingLog.Start(c, null, null, null, null, null)], () => new Cams.GetViewingLogsQuery()),
        new("Camps", c => [Camp.Create(c, "YAZ", "Yaz")], () => new CampQ.GetCampsQuery()),
        new("Institutions", c => [Inst(c)], () => new Cons.GetInstitutionsQuery()),
        new("ConsultancyPlans", PlanFirst, () => new Cons.GetConsultancyPlansQuery()),
        new("SchoolVisits", c => WithInstitution(c, i => SchoolVisit.Schedule(c, i, D)), () => new Cons.GetSchoolVisitsQuery()),
        new("ConsultancyAgreements", c => WithPlan(c, (p, i) => ConsultancyAgreement.Create(c, p, i, "Sözleşme")),
            () => new Cons.GetConsultancyAgreementsQuery()),
        new("ConsultancyReports", c => WithPlan(c, (p, _) => ConsultancyReport.Create(c, "Rapor", p, null, "Özet")),
            () => new Cons.GetConsultancyReportsQuery()),
        new("FollowUpActivities", c => WithPlan(c, (p, _) => FollowUpActivity.Create(c, "Takip", p, null)),
            () => new Cons.GetFollowUpActivitiesQuery()),
        new("Invoices", c => [Invoice.Create(c, D)], () => new Fin.GetInvoicesQuery()),
        new("Payments", c => [Payment.Record(c, 100m)], () => new Fin.GetPaymentsQuery()),
        new("Promotions", c => [Promotion.Create(c, "YAZ10", "Yaz", 10m)], () => new Fin.GetPromotionsQuery()),
        new("PackageDefinitions", c => [PackageDefinition.Create(c, "P10", "10 Seans", 1000m)], () => new Fin.GetPackageDefinitionsQuery()),
        new("StudentPackages", c => WithStudent(c, s => StudentPackage.Purchase(c, s, 10m, 1000m)), () => new Fin.GetStudentPackagesQuery()),
        new("Scholarships", c => WithStudent(c, s => Scholarship.Grant(c, s, percentage: 10m)), () => new Fin.GetScholarshipsQuery()),
        new("GoalLibraries", c => [GoalLibrary.Create(c, "Dil")], () => new Goal.GetGoalLibrariesQuery()),
        new("GoalTemplates", c => [GoalTemplate.Create(c, "Hedef")], () => new Goal.GetGoalTemplatesQuery()),
        new("StudentGoals", c => WithStudent(c, s => StudentGoal.Create(c, s, "Hedef")), () => new Goal.GetStudentGoalsQuery()),
        new("Leads", c => [Lead.Create(c, "Veli")], () => new LeadQ.GetLeadsQuery()),
        new("LeaveRequests", c => { var e = Educator.Create(c, "Zeynep", "Ak"); return [LeaveRequest.Submit(c, e.Id, null, "day", T, T.AddDays(1), 1m, null), e]; },
            () => new Leave.GetLeaveRequestsQuery()),
        new("Meetings", c => [Meeting.Create(c, "Toplantı")], () => new Meet.GetMeetingsQuery()),
        new("AcademicPeriods", c => [AcademicPeriod.Create(c, "2026", D, D.AddMonths(9))], () => new Plan.GetAcademicPeriodsQuery()),
        new("EducationPlans", c => WithStudent(c, s => EducationPlan.Create(c, s, "BEP")), () => new Plan.GetEducationPlansQuery()),
        new("Programs", c => [EducationProgram.Create(c, "DKT", "Dil")], () => new Prog.GetProgramsQuery()),
        new("StudentEnrollments", c => WithStudent(c, s => Enrollment.Create(c, s, null, null)), () => new Prog.GetStudentEnrollmentsQuery()),
        new("Rooms", c => [Room.Create(c, "R1", "Derslik", 4, false)], () => new Sched.GetRoomsQuery()),
        new("Sessions", c => [Session.Schedule(c, Guid.CreateVersion7(), T, T.AddMinutes(40))], () => new Sched.GetSessionsQuery()),
        new("MakeupRequests", c => WithStudent(c, s => MakeupRequest.Create(c, s)), () => new Sched.GetMakeupRequestsQuery()),
        new("RecurringSchedules", c => [RecurringSchedule.Create(c, "weekly", new TimeOnly(10, 0), 40, D)], () => new Sched.GetRecurringSchedulesQuery()),
        new("CalendarEntries", c => [CalendarEntry.Create(c, "Tatil", "holiday", T, T.AddDays(1))],
            () => new Sched.GetCalendarEntriesQuery(T.AddDays(-1), T.AddDays(2), null)),
        new("Surveys", c => [SurveyDefinition.Create(c, "Memnuniyet")], () => new Surv.GetSurveysQuery()),
        new("ParentFeedback", c => [ParentFeedback.Create(c, null, null, null, 5, null)], () => new Kpi.GetParentFeedbackQuery()),
        new("ConsentTemplates", c => [ConsentTemplate.Create(c, "KVKK")], () => new Legal.GetConsentTemplatesQuery()),
        new("ContractTemplates", c => [ContractTemplate.Create(c, "HIZMET")], () => new Legal.GetContractTemplatesQuery()),
        new("StudentConsents", c => WithStudent(c, s => StudentConsent.Grant(c, s)), () => new Legal.GetStudentConsentsQuery()),
        new("StudentContracts", c => WithStudent(c, s => StudentContract.Generate(c, s)), () => new Legal.GetStudentContractsQuery()),
        new("AssessmentTemplates", c => [AssessmentTemplate.Create("T1", "Test", c)], () => new Assess.GetAssessmentTemplatesQuery()),
        new("AssessmentSessions", c => { var t = AssessmentTemplate.Create("T1", "Test", c); return [AssessmentSession.Create(c, t.Id, 1, null, Guid.CreateVersion7()), t]; },
            () => new Assess.GetAssessmentSessionsQuery()),
        new("ConsentTemplateVersions", c => [ConsentTemplate.Create(c, "KVKK")], () => new Legal.GetConsentTemplateVersionsQuery("KVKK")),
        new("ContractTemplateVersions", c => [ContractTemplate.Create(c, "HIZMET")], () => new Legal.GetContractTemplateVersionsQuery("HIZMET")),
        new("LeaveBalances", c => { var e = Educator.Create(c, "Zeynep", "Ak"); return [LeaveBalance.Initialize(c, e.Id, null, 2026, 14m), e]; },
            () => new Leave.GetLeaveBalancesQuery()),
        new("NotificationTemplates", c => [new NotificationTemplate { CorporationId = c, Code = "SEANS" }], () => new Notif.GetNotificationTemplatesQuery()),
        new("TriggerConfigs", c => [NotificationTriggerConfig.Create("session_reminder", null, 60, c)], () => new Notif.GetTriggerConfigsQuery()),
        new("KpiDefinitions", c => [new KpiDefinition { CorporationId = c, Code = "ATT", Name = "Devam", Spec = "{}" }],
            () => new Kpi.GetKpiDefinitionsQuery { IncludePlatform = false }),
        new("StudentPrograms", c =>
        {
            var s = St(c); var p = EducationProgram.Create(c, "DKT", "Dil");
            return [StudentProgram.Create(c, s.Id, p.Id), s, p];
        }, () => new Prog.GetStudentProgramsQuery()),
    ];

    [Fact]
    public async Task GetCreditSummary_rejects_student_of_another_tenant()
    {
        using var ctx = new TenantTestContext();
        var foreignStudent = St(TenantB);
        await ctx.SeedAsync(foreignStudent);

        await ShouldRejectAsync(() => ctx.Handler<Fin.GetCreditSummaryQueryHandler>().Handle(
            new Fin.GetCreditSummaryQuery(foreignStudent.Id), default));
    }

    private static object[] PlanFirst(Guid c)
    {
        var i = Inst(c);
        return [ConsultancyPlan.Create(c, i.Id, "Plan"), i];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Query_returns_only_rows_of_the_current_tenant(Case @case)
    {
        using var ctx = new TenantTestContext();
        var mine = @case.Seed(TenantA);
        var foreign = @case.Seed(TenantB);
        await ctx.SeedAsync([.. mine, .. foreign]);

        var query = @case.Query();
        var handlerType = typeof(ITenantContext).Assembly.GetTypes().Single(t =>
            t is { IsClass: true, IsAbstract: false } &&
            t.GetInterfaces().Any(i => i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                && i.GetGenericArguments()[0] == query.GetType()));
        var handler = ctx.Handler(handlerType);
        var handle = handlerType.GetMethod("Handle", [query.GetType(), typeof(CancellationToken)])!;

        var task = (Task)handle.Invoke(handler, [query, CancellationToken.None])!;
        await task;
        var result = task.GetType().GetProperty("Result")!.GetValue(task)!;

        var items = (IEnumerable)(result.GetType().GetProperty("Items")?.GetValue(result) ?? result);
        var ids = items.Cast<object>().Select(i => i.GetType().GetProperty("Id")!.GetValue(i)).ToList();

        var expected = mine[0].GetType().GetProperty("Id")!.GetValue(mine[0]);
        ids.Should().ContainSingle().Which.Should().Be(expected, $"{@case.Name} must list tenant A's row only");
    }
}
