namespace Aynesil.Application.Features.Legal.Dtos;

// ── ContractTemplate DTOs ──────────────────────────────────────────────────────

public record ContractTemplateTranslationDto(
    string Locale,
    string Title,
    string Body);

public record ContractTemplateListItemDto(
    Guid Id,
    Guid CorporationId,
    string Code,
    Guid? ContractTypeId,
    string? ContractTypeCode,
    int Version,
    bool IsCurrent,
    DateOnly? EffectiveFrom,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record ContractTemplateDto(
    Guid Id,
    Guid CorporationId,
    string Code,
    Guid? ContractTypeId,
    string? ContractTypeCode,
    int Version,
    bool IsCurrent,
    DateOnly? EffectiveFrom,
    IReadOnlyList<ContractTemplateTranslationDto> Translations,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);

// ── ConsentTemplate DTOs ──────────────────────────────────────────────────────

public record ConsentTemplateTranslationDto(
    string Locale,
    string Title,
    string Body);

public record ConsentTemplateListItemDto(
    Guid Id,
    Guid CorporationId,
    string Code,
    Guid? ConsentTypeId,
    string? ConsentTypeCode,
    int Version,
    bool IsCurrent,
    bool IsMandatory,
    DateOnly? EffectiveFrom,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record ConsentTemplateDto(
    Guid Id,
    Guid CorporationId,
    string Code,
    Guid? ConsentTypeId,
    string? ConsentTypeCode,
    int Version,
    bool IsCurrent,
    bool IsMandatory,
    DateOnly? EffectiveFrom,
    IReadOnlyList<ConsentTemplateTranslationDto> Translations,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int RowVersion);

// ── StudentContract DTOs ──────────────────────────────────────────────────────

public record StudentContractListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid StudentId,
    string? StudentFullName,
    Guid? TemplateId,
    string? TemplateCode,
    int? TemplateVersion,
    Guid? GuardianId,
    string Status,
    DateTimeOffset? SignedAt,
    string? SignatureMethod,
    DateOnly? StartsOn,
    DateOnly? EndsOn,
    DateTimeOffset CreatedAt);

public record StudentContractDto(
    Guid Id,
    Guid CorporationId,
    Guid StudentId,
    string? StudentFullName,
    Guid? TemplateId,
    string? TemplateCode,
    int? TemplateVersion,
    Guid? GuardianId,
    string Status,
    DateTimeOffset? SignedAt,
    string? SignedByName,
    string? SignatureMethod,
    string? SignatureRef,
    Guid? SignedFileId,
    DateOnly? StartsOn,
    DateOnly? EndsOn,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy,
    DateTimeOffset UpdatedAt,
    int RowVersion);

// ── StudentConsent DTOs ───────────────────────────────────────────────────────

public record StudentConsentListItemDto(
    Guid Id,
    Guid CorporationId,
    Guid StudentId,
    string? StudentFullName,
    Guid? ConsentTypeId,
    string? ConsentTypeCode,
    Guid? TemplateId,
    string? TemplateCode,
    int? TemplateVersion,
    Guid? GuardianId,
    string State,
    DateTimeOffset? GrantedAt,
    DateTimeOffset? WithdrawnAt,
    DateOnly? ValidUntil,
    bool HasEvidence,
    DateTimeOffset CreatedAt);

public record StudentConsentDto(
    Guid Id,
    Guid CorporationId,
    Guid StudentId,
    string? StudentFullName,
    Guid? ConsentTypeId,
    string? ConsentTypeCode,
    Guid? TemplateId,
    string? TemplateCode,
    int? TemplateVersion,
    Guid? GuardianId,
    string State,
    DateTimeOffset? GrantedAt,
    DateTimeOffset? WithdrawnAt,
    DateOnly? ValidUntil,
    Guid? EvidenceFileId,
    DateTimeOffset CreatedAt,
    Guid? CreatedBy,
    DateTimeOffset UpdatedAt,
    int RowVersion);

// ── Report DTOs ───────────────────────────────────────────────────────────────

/// <summary>Contract summary line for the Contract Report. One row per student.</summary>
public record ContractReportItemDto(
    Guid StudentId,
    string StudentFullName,
    int TotalContracts,
    int DraftContracts,
    int ActiveContracts,
    int ExpiredContracts,
    int TerminatedContracts,
    DateTimeOffset? LatestSignedAt);

/// <summary>Consent compliance summary. One row per student per consent type.</summary>
public record ConsentReportItemDto(
    Guid StudentId,
    string StudentFullName,
    Guid? ConsentTypeId,
    string? ConsentTypeCode,
    bool HasGrantedConsent,
    DateTimeOffset? GrantedAt,
    DateTimeOffset? WithdrawnAt,
    DateOnly? ValidUntil,
    bool IsMandatory);

/// <summary>Signature tracking for digital-signature readiness report.</summary>
public record SignatureReportItemDto(
    Guid ContractId,
    Guid StudentId,
    string StudentFullName,
    string Status,
    string? SignatureMethod,
    string? SignatureRef,
    bool HasSignedFile,
    DateTimeOffset? SignedAt,
    string? SignedByName);
