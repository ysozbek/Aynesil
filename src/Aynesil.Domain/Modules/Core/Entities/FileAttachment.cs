namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.file_attachment.
/// Polymorphic join between a FileObject and any owning entity
/// (owner_schema + owner_table + owner_id). Avoids FK columns on every entity.
/// One file can be attached to multiple owners with different purposes.
/// </summary>
public class FileAttachment : TenantEntity
{
    public Guid FileId { get; set; }

    /// <summary>PostgreSQL schema of the owning entity, e.g. 'students'.</summary>
    public string OwnerSchema { get; set; } = string.Empty;

    /// <summary>Table name of the owning entity, e.g. 'medical_report'.</summary>
    public string OwnerTable { get; set; } = string.Empty;

    /// <summary>Primary key of the owning record.</summary>
    public Guid OwnerId { get; set; }

    /// <summary>Semantic label for this attachment: 'avatar', 'report_pdf', 'signed_contract'.</summary>
    public string? Purpose { get; set; }

    public FileObject? File { get; set; }
}
