using Aynesil.Domain.Common;

namespace Aynesil.Domain.Modules.Educators.Entities;

/// <summary>
/// Professional certification held by an educator.
/// Certification type is configurable reference data (ref_type 'certification_type').
/// An optional file_id stores the uploaded certificate document.
/// Maps to educators.educator_certification.
///
/// Audit: created_at, updated_at only (created_by/updated_by absent from DDL).
/// Soft delete: deleted_at.
/// Concurrency: row_version.
/// </summary>
public class EducatorCertification : TenantEntity
{
    public Guid EducatorId { get; private set; }

    /// <summary>FK to ref.ref_value (ref_type 'certification_type'). Configurable.</summary>
    public Guid? CertificationTypeId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Issuer { get; private set; }

    public DateOnly? IssuedOn { get; private set; }

    /// <summary>NULL means the certification does not expire.</summary>
    public DateOnly? ExpiresOn { get; private set; }

    /// <summary>FK to core.file_object — uploaded certificate scan/document.</summary>
    public Guid? FileId { get; private set; }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static EducatorCertification Create(
        Guid corporationId,
        Guid educatorId,
        string name,
        Guid? certificationTypeId = null,
        string? issuer = null,
        DateOnly? issuedOn = null,
        DateOnly? expiresOn = null,
        Guid? fileId = null)
    {
        return new EducatorCertification
        {
            CorporationId       = corporationId,
            EducatorId          = educatorId,
            Name                = name,
            CertificationTypeId = certificationTypeId,
            Issuer              = issuer,
            IssuedOn            = issuedOn,
            ExpiresOn           = expiresOn,
            FileId              = fileId,
            CreatedAt           = DateTimeOffset.UtcNow,
            UpdatedAt           = DateTimeOffset.UtcNow
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void Update(
        string name,
        Guid? certificationTypeId,
        string? issuer,
        DateOnly? issuedOn,
        DateOnly? expiresOn,
        Guid? fileId)
    {
        Name                = name;
        CertificationTypeId = certificationTypeId;
        Issuer              = issuer;
        IssuedOn            = issuedOn;
        ExpiresOn           = expiresOn;
        FileId              = fileId;
        UpdatedAt           = DateTimeOffset.UtcNow;
    }

    /// <summary>Returns true when the certification has a known expiry and is past it.</summary>
    public bool IsExpired => ExpiresOn.HasValue && ExpiresOn.Value < DateOnly.FromDateTime(DateTime.UtcNow);
}
