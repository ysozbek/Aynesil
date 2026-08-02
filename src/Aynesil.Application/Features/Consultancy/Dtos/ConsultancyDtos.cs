namespace Aynesil.Application.Features.Consultancy.Dtos;

// ── Institution DTOs ──────────────────────────────────────────────────────────

public record InstitutionListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid? InstitutionTypeId,
    string? InstitutionTypeCode,
    string Name,
    string? City,
    string? District,
    int PlanCount,
    int VisitCount,
    DateTimeOffset CreatedAt);

public record InstitutionDto(
    Guid Id,
    Guid CorporationId,
    Guid? InstitutionTypeId,
    string? InstitutionTypeCode,
    string Name,
    string? City,
    string? District,
    string? ContactName,
    string? ContactPhone,
    string? ContactEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);

// ── ConsultancyPlan DTOs ──────────────────────────────────────────────────────

public record ConsultancyPlanListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid InstitutionId,
    string InstitutionName,
    Guid? ConsultancyTypeId,
    string? ConsultancyTypeCode,
    string Name,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    string Status,
    int VisitCount,
    int ReportCount,
    DateTimeOffset CreatedAt);

public record ConsultancyPlanDto(
    Guid Id,
    Guid CorporationId,
    Guid InstitutionId,
    string InstitutionName,
    Guid? ConsultancyTypeId,
    string? ConsultancyTypeCode,
    string Name,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    string? Scope,
    Guid? LeadEducatorId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);

// ── SchoolVisit DTOs ──────────────────────────────────────────────────────────

public record SchoolVisitListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid? ConsultancyPlanId,
    string? PlanName,
    Guid InstitutionId,
    string InstitutionName,
    DateOnly VisitDate,
    Guid? VisitorId,
    string? Purpose,
    string Status,
    int ObservationCount,
    DateTimeOffset CreatedAt);

public record SchoolVisitDto(
    Guid Id,
    Guid CorporationId,
    Guid? ConsultancyPlanId,
    string? PlanName,
    Guid InstitutionId,
    string InstitutionName,
    DateOnly VisitDate,
    Guid? VisitorId,
    string? Purpose,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ObservationRecordDto> Observations);

// ── ObservationRecord DTOs ────────────────────────────────────────────────────

public record ObservationRecordDto(
    Guid Id,
    Guid CorporationId,
    Guid SchoolVisitId,
    Guid? ObservationTypeId,
    string? ObservationTypeCode,
    string? Subject,
    string Observation,
    string? Recommendations,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy);

// ── ConsultancyReport DTOs ────────────────────────────────────────────────────

public record ConsultancyReportListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid? ConsultancyPlanId,
    string? PlanName,
    Guid? SchoolVisitId,
    DateOnly? VisitDate,
    string Title,
    bool HasFile,
    Guid? AuthoredBy,
    DateTimeOffset CreatedAt);

public record ConsultancyReportDto(
    Guid Id,
    Guid CorporationId,
    Guid? ConsultancyPlanId,
    string? PlanName,
    Guid? SchoolVisitId,
    DateOnly? VisitDate,
    string Title,
    string? Summary,
    Guid? FileId,
    Guid? AuthoredBy,
    DateTimeOffset CreatedAt);

// ── Reporting DTOs ────────────────────────────────────────────────────────────

/// <summary>Institution-level activity summary for the Institution Report.</summary>
public record InstitutionReportDto(
    Guid InstitutionId,
    string InstitutionName,
    string? InstitutionTypeCode,
    string? City,
    int TotalPlans,
    int ActivePlans,
    int CompletedPlans,
    int TotalVisits,
    int CompletedVisits,
    int TotalObservations,
    int TotalReports);

/// <summary>Plan-level outcomes summary for the Consultancy Outcomes Report.</summary>
public record ConsultancyOutcomesDto(
    Guid PlanId,
    string PlanName,
    string InstitutionName,
    string? ConsultancyTypeCode,
    DateOnly? PeriodStart,
    DateOnly? PeriodEnd,
    string Status,
    int VisitCount,
    int CompletedVisitCount,
    int ObservationCount,
    int ReportCount);

/// <summary>Visit history line item for the Visit History Report.</summary>
public record VisitHistoryItemDto(
    Guid VisitId,
    Guid InstitutionId,
    string InstitutionName,
    string? PlanName,
    DateOnly VisitDate,
    string? Purpose,
    string Status,
    int ObservationCount,
    int ReportCount,
    DateTimeOffset CreatedAt);
