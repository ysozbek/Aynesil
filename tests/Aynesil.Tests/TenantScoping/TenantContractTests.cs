using System.Reflection;
using System.Runtime.CompilerServices;
using Aynesil.Application.Common.Interfaces;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Aynesil.Tests.TenantScoping;

/// <summary>
/// Contract tests that cover every client-facing MediatR request at once:
///   1. No request type exposes CorporationId or an actor id (CreatedBy, UpdatedBy, ...), so the
///      client cannot supply them.
///   2. Every tenant-scoped handler reads the corporation from ITenantContext: with no tenant it
///      fails with UnauthorizedAccessException (mapped to 401) before touching any data.
/// Handler-specific behaviour (persisted CorporationId, cross-tenant parent rejection) is covered
/// by the per-module test classes in this folder.
/// </summary>
public class TenantContractTests
{
    private static readonly Assembly ApplicationAssembly = typeof(ITenantContext).Assembly;

    /// <summary>Server-internal requests that are never bound from an HTTP request.</summary>
    private static readonly HashSet<string> ServerInternalRequests = ["SendNotificationCommand"];

    private static readonly string[] ForbiddenRequestMembers =
    [
        "CorporationId", "CreatedBy", "UpdatedBy", "DeletedBy", "AuthoredBy",
        "RecordedBy", "AssignedBy", "CompletedBy", "GrantedBy", "RevokedBy", "RespondentUserId",
        // Onay/karar aktörleri (Y5): oturumdaki kullanıcıdan ya da ona bağlı eğitmen kaydından türetilir.
        "ApproverId", "ApprovedBy", "RejectedBy", "ReviewedBy", "DecidedBy", "VerifiedBy", "VoidedBy",
        "ProcessedBy", "CancelledBy", "SignedBy", "IssuedBy", "FinalizedBy", "RevisedBy", "ChangedBy",
        "UploadedBy", "RequestedBy",
    ];

    /// <summary>Requests whose CorporationId was moved from the payload to ITenantContext.</summary>
    private static readonly string[] TenantScopedRequests =
    [
        "AddMeetingFollowUpCommand",
        "CompleteInterviewCommand",
        "LogLeadActivityCommand",
        "AddMeetingParticipantCommand",
        "ApplyDiscountCommand",
        "AssignCameraToRoomCommand",
        "AssignCameraToSessionCommand",
        "AssignCampEducatorCommand",
        "AssignCareTeamMemberCommand",
        "AssignStudentToProgramCommand",
        "BulkComputeSnapshotsCommand",
        "BulkRecordAttendanceCommand",
        "BulkRegisterParticipationCommand",
        "CarryForwardLeaveBalanceCommand",
        "ComputePerformanceSnapshotCommand",
        "CreateAcademicPeriodCommand",
        "CreateAssessmentReportCommand",
        "CreateAssessmentSessionCommand",
        "CreateAssessmentTemplateCommand",
        "CreateCalendarEntryCommand",
        "CreateCampActivityCommand",
        "CreateCampCommand",
        "CreateCampPeriodCommand",
        "CreateCampReportCommand",
        "CreateCampusCommand",
        "CreateConsentTemplateCommand",
        "CreateConsultancyAgreementCommand",
        "CreateConsultancyPlanCommand",
        "CreateConsultancyReportCommand",
        "CreateContractTemplateCommand",
        "CreateEducationPlanCommand",
        "CreateEducatorCommand",
        "CreateEnrollmentCommand",
        "CreateFollowUpActivityCommand",
        "CreateGoalLibraryCommand",
        "CreateGoalTemplateCommand",
        "CreateGuardianCommand",
        "CreateInstitutionCommand",
        "CreateInvoiceCommand",
        "CreateKpiDefinitionCommand",
        "CreateLeadCommand",
        "CreateNotificationTemplateCommand",
        "CreateObservationCommand",
        "CreatePackageDefinitionCommand",
        "CreateParentFeedbackCommand",
        "CreateProgramCommand",
        "CreateProgramRecommendationCommand",
        "CreatePromotionCommand",
        "CreateRecurringScheduleCommand",
        "CreateRoomCommand",
        "CreateStudentCommand",
        "CreateStudentGoalCommand",
        "CreateSurveyDefinitionCommand",
        "EndViewingSessionCommand",
        "EnrollStudentCommand",
        "GenerateStudentContractCommand",
        "GetAcademicPeriodsQuery",
        "GetAgreementSummaryQuery",
        "GetAssessmentSessionsQuery",
        "GetAssessmentTemplatesQuery",
        "GetAttendanceSummaryQuery",
        "GetCalendarEntriesQuery",
        "GetCamerasQuery",
        "GetCampPerformanceQuery",
        "GetCampsQuery",
        "GetCampusCalendarQuery",
        "GetCampusesQuery",
        "GetConsentReportQuery",
        "GetConsentTemplateVersionsQuery",
        "GetConsentTemplatesQuery",
        "GetConsultancyAgreementsQuery",
        "GetConsultancyOutcomesQuery",
        "GetConsultancyPlansQuery",
        "GetConsultancyReportsQuery",
        "GetContractReportQuery",
        "GetContractTemplateVersionsQuery",
        "GetContractTemplatesQuery",
        "GetConversionReportQuery",
        "GetCorporationKpiTrendQuery",
        "GetCreditLedgerQuery",
        "GetCreditSummaryQuery",
        "GetCreditUsageReportQuery",
        "GetEducationPlansQuery",
        "GetEducatorCalendarQuery",
        "GetEducatorDashboardQuery",
        "GetEducatorKpiTrendQuery",
        "GetEducatorKpiValuesQuery",
        "GetEducatorRankingQuery",
        "GetEducatorUtilizationQuery",
        "GetEducatorsQuery",
        "GetExecutiveDashboardQuery",
        "GetFollowUpActivitiesQuery",
        "GetFollowUpsDueQuery",
        "GetGoalLibrariesQuery",
        "GetGoalSuccessRatesQuery",
        "GetGoalTemplatesQuery",
        "GetGuardianVisiblePlanQuery",
        "GetGuardiansQuery",
        "GetInstitutionReportQuery",
        "GetInstitutionsQuery",
        "GetInvoicesQuery",
        "GetKpiCategoriesQuery",
        "GetKpiDefinitionsQuery",
        "GetKpiReportQuery",
        "GetKpiValuesQuery",
        "GetLeadsQuery",
        "GetLeaveBalancesQuery",
        "GetLeaveCalendarQuery",
        "GetLeaveRequestsQuery",
        "GetLeaveTrendsQuery",
        "GetLeaveUsageReportQuery",
        "GetMakeupRequestsQuery",
        "GetManagerDashboardQuery",
        "GetMeetingCalendarQuery",
        "GetMeetingsQuery",
        "GetNotificationTemplatesQuery",
        "GetOpenFollowUpsReportQuery",
        "GetPackageDefinitionsQuery",
        "GetPackageReportQuery",
        "GetParentFeedbackQuery",
        "GetPaymentsQuery",
        "GetPerformanceSnapshotsQuery",
        "GetPipelineSummaryQuery",
        "GetProgramsQuery",
        "GetPromotionsQuery",
        "GetRecurringSchedulesQuery",
        "GetRefundsQuery",
        "GetRevenueReportQuery",
        "GetRoomsQuery",
        "GetScholarshipsQuery",
        "GetSchoolCalendarQuery",
        "GetSchoolVisitsQuery",
        "GetSessionsQuery",
        "GetSignatureReportQuery",
        "GetStudentAttendanceQuery",
        "GetStudentCalendarQuery",
        "GetStudentConsentsQuery",
        "GetStudentContractsQuery",
        "GetStudentEnrollmentsQuery",
        "GetStudentGoalSummaryQuery",
        "GetStudentGoalSummaryReportQuery",
        "GetStudentGoalsQuery",
        "GetStudentPackagesQuery",
        "GetStudentProgramsQuery",
        "GetStudentsQuery",
        "GetSurveysQuery",
        "GetTrendReportQuery",
        "GetTriggerConfigsQuery",
        "GetViewingAuthorizationsQuery",
        "GetViewingLogsQuery",
        "GetVisitHistoryQuery",
        "GrantConsentCommand",
        "GrantScholarshipCommand",
        "GrantViewingAuthorizationCommand",
        "InitializeLeaveBalanceCommand",
        "PurchaseStudentPackageCommand",
        "RecordAttendanceCommand",
        "RecordMeetingOutcomeCommand",
        "RecordPaymentCommand",
        "RegisterCameraCommand",
        "RegisterParticipationCommand",
        "RequestMakeupCommand",
        "ScheduleMeetingCommand",
        "ScheduleSchoolVisitCommand",
        "ScheduleSessionCommand",
        "StartViewingSessionCommand",
        "SubmitLeaveRequestCommand",
        "SubmitParentFeedbackCommand",
        "SubmitSurveyResponseCommand",
        "UpdateNotificationPreferencesCommand",
        "UpsertNotificationTriggerConfigCommand",
        "ValidatePromotionCodeQuery"
    ];

    public static TheoryData<string> TenantScopedRequestNames()
    {
        var data = new TheoryData<string>();
        foreach (var name in TenantScopedRequests) data.Add(name);
        return data;
    }

    [Fact]
    public void No_request_exposes_CorporationId_or_actor_ids()
    {
        var offenders = ApplicationAssembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && IsRequest(t))
            .Where(t => !ServerInternalRequests.Contains(t.Name))
            .SelectMany(t => t.GetProperties()
                .Where(p => ForbiddenRequestMembers.Contains(p.Name))
                .Select(p => $"{t.Name}.{p.Name}"))
            .ToList();

        offenders.Should().BeEmpty("CorporationId and actor ids must come from the request context");
    }

    [Theory]
    [MemberData(nameof(TenantScopedRequestNames))]
    public async Task Handler_requires_tenant_context(string requestName)
    {
        // Some names exist in two modules (e.g. RecordAttendanceCommand in Camps and Scheduling);
        // the tenant-scoped one is the variant whose handler depends on ITenantContext.
        var candidates = ApplicationAssembly.GetTypes()
            .Where(t => t.Name == requestName)
            .Select(t => (Request: t, Handler: FindHandler(t)))
            .Where(x => x.Handler is not null && TakesTenant(x.Handler))
            .ToList();
        candidates.Should().NotBeEmpty($"{requestName} should have a handler that depends on ITenantContext");

        foreach (var (requestType, handlerType) in candidates)
        {
            using var ctx = new TenantTestContext();
            ctx.Tenant.SetupGet(t => t.CorporationId).Returns((Guid?)null);

            var handler = CreateHandler(handlerType!, ctx);
            var request = RuntimeHelpers.GetUninitializedObject(requestType);
            var handle = handlerType!.GetMethod("Handle", [requestType, typeof(CancellationToken)])!;

            var act = async () => await (Task)handle.Invoke(handler, [request, CancellationToken.None])!;

            await act.Should().ThrowAsync<UnauthorizedAccessException>(
                $"{requestType.FullName} must resolve CorporationId from ITenantContext");
        }
    }

    private static Type? FindHandler(Type requestType) =>
        ApplicationAssembly.GetTypes().SingleOrDefault(t =>
            t is { IsClass: true, IsAbstract: false } &&
            t.GetInterfaces().Any(i => i.IsGenericType
                && (i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                    || i.GetGenericTypeDefinition() == typeof(IRequestHandler<>))
                && i.GetGenericArguments()[0] == requestType));

    private static bool TakesTenant(Type handlerType) =>
        handlerType.GetConstructors().Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(ITenantContext)));

    private static bool IsRequest(Type t) =>
        t.GetInterfaces().Any(i => i == typeof(IRequest)
            || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)));

    private static object CreateHandler(Type handlerType, TenantTestContext ctx)
    {
        var ctor = handlerType.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var args = ctor.GetParameters().Select(p => Resolve(p.ParameterType, ctx)).ToArray();
        return ctor.Invoke(args);
    }

    private static object Resolve(Type type, TenantTestContext ctx)
    {
        if (type == typeof(IAppDbContext)) return ctx.Db;
        if (type == typeof(ITenantContext)) return ctx.Tenant.Object;
        if (type == typeof(ICurrentUserService)) return ctx.CurrentUser.Object;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ILogger<>))
            return Activator.CreateInstance(typeof(NullLogger<>).MakeGenericType(type.GetGenericArguments()))!;
        var mock = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(type))!;
        return mock.Object;
    }
}
