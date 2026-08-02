namespace Aynesil.Application.Features.Consultancy.Dtos;

// ── ConsultancyAgreement DTOs ─────────────────────────────────────────────────

public record ConsultancyAgreementListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid ConsultancyPlanId,
    string PlanName,
    Guid InstitutionId,
    string InstitutionName,
    Guid? AgreementTypeId,
    string? AgreementTypeCode,
    string Title,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateOnly? SignedDate,
    string Status,
    bool HasFile,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record ConsultancyAgreementDto(
    Guid Id,
    Guid CorporationId,
    Guid ConsultancyPlanId,
    string PlanName,
    Guid InstitutionId,
    string InstitutionName,
    Guid? AgreementTypeId,
    string? AgreementTypeCode,
    string Title,
    string? Description,
    DateOnly? StartDate,
    DateOnly? EndDate,
    DateOnly? SignedDate,
    string Status,
    Guid? FileId,
    string? SignedByName,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy,
    DateTimeOffset UpdatedAt,
    Guid? UpdatedBy,
    int RowVersion);

// ── FollowUpActivity DTOs ─────────────────────────────────────────────────────

public record FollowUpActivityListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid? ConsultancyPlanId,
    string? PlanName,
    Guid? SchoolVisitId,
    DateOnly? VisitDate,
    Guid? ObservationRecordId,
    string Title,
    DateOnly? DueDate,
    Guid? AssignedTo,
    string Status,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt);

public record FollowUpActivityDto(
    Guid Id,
    Guid CorporationId,
    Guid? ConsultancyPlanId,
    string? PlanName,
    Guid? SchoolVisitId,
    DateOnly? VisitDate,
    Guid? ObservationRecordId,
    string Title,
    string? Description,
    DateOnly? DueDate,
    Guid? AssignedTo,
    string Status,
    DateTimeOffset? CompletedAt,
    Guid? CompletedBy,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);

// ── Reporting DTOs ─────────────────────────────────────────────────────────────

/// <summary>Agreement status summary per consultancy plan.</summary>
public record AgreementSummaryDto(
    Guid PlanId,
    string PlanName,
    string InstitutionName,
    int TotalAgreements,
    int DraftCount,
    int SentCount,
    int SignedCount,
    int ExpiredCount,
    int CancelledCount);

/// <summary>Open follow-up activities report — overdue or upcoming tasks.</summary>
public record OpenFollowUpReportItemDto(
    Guid ActivityId,
    string Title,
    Guid? ConsultancyPlanId,
    string? PlanName,
    Guid? SchoolVisitId,
    DateOnly? VisitDate,
    DateOnly? DueDate,
    bool IsOverdue,
    Guid? AssignedTo,
    string Status,
    DateTimeOffset CreatedAt);
