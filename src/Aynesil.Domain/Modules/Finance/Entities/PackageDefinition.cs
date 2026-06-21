namespace Aynesil.Domain.Modules.Finance.Entities;

/// <summary>
/// Package catalog entry defining what can be sold to a student:
/// session packages, program packages, or custom packages.
/// package_type_id references ref.ref_value (ref_type 'package_type') — configurable.
/// program_id optionally ties the package to a specific education.program.
///
/// Maps to finance.package_definition.
/// Audit: created_at, updated_at, deleted_at, row_version (no created_by / updated_by in DDL).
/// </summary>
public class PackageDefinition : TenantEntity
{
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    /// <summary>FK to ref.ref_value (ref_type 'package_type'). Configurable.</summary>
    public Guid? PackageTypeId { get; private set; }

    /// <summary>Optional link to education.program for program packages.</summary>
    public Guid? ProgramId { get; private set; }

    /// <summary>Number of sessions (or credit units) included. Null = unlimited.</summary>
    public decimal? TotalCredits { get; private set; }

    /// <summary>Days from purchase until the package expires. Null = no expiry.</summary>
    public int? ValidityDays { get; private set; }

    public decimal ListPrice { get; private set; }
    public string Currency { get; private set; } = "TRY";
    public bool IsActive { get; private set; } = true;

    // ── Navigations ───────────────────────────────────────────────────────────

    public ICollection<StudentPackage> StudentPackages { get; private set; } = [];

    // ── Factory ───────────────────────────────────────────────────────────────

    public static PackageDefinition Create(
        Guid corporationId,
        string code,
        string name,
        decimal listPrice,
        Guid? packageTypeId = null,
        Guid? programId = null,
        decimal? totalCredits = null,
        int? validityDays = null,
        string currency = "TRY")
    {
        if (listPrice < 0)
            throw new ArgumentException("List price cannot be negative.");

        if (totalCredits is not null && totalCredits <= 0)
            throw new ArgumentException("Total credits must be positive when specified.");

        if (validityDays is not null && validityDays <= 0)
            throw new ArgumentException("Validity days must be positive when specified.");

        return new PackageDefinition
        {
            CorporationId = corporationId,
            Code          = code,
            Name          = name,
            ListPrice     = listPrice,
            PackageTypeId = packageTypeId,
            ProgramId     = programId,
            TotalCredits  = totalCredits,
            ValidityDays  = validityDays,
            Currency      = currency,
            IsActive      = true,
            CreatedAt     = DateTimeOffset.UtcNow,
            UpdatedAt     = DateTimeOffset.UtcNow
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────

    public void UpdateDetails(
        string code,
        string name,
        decimal listPrice,
        Guid? packageTypeId,
        Guid? programId,
        decimal? totalCredits,
        int? validityDays,
        string currency)
    {
        if (listPrice < 0)
            throw new ArgumentException("List price cannot be negative.");

        if (totalCredits is not null && totalCredits <= 0)
            throw new ArgumentException("Total credits must be positive when specified.");

        Code          = code;
        Name          = name;
        ListPrice     = listPrice;
        PackageTypeId = packageTypeId;
        ProgramId     = programId;
        TotalCredits  = totalCredits;
        ValidityDays  = validityDays;
        Currency      = currency;
        UpdatedAt     = DateTimeOffset.UtcNow;
    }

    public void Activate()   { IsActive = true;  UpdatedAt = DateTimeOffset.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTimeOffset.UtcNow; }
}
