namespace Aynesil.Domain.Modules.Core.Entities;

/// <summary>
/// Maps to core.file_object.
/// Metadata record for an uploaded file. Business entities reference file IDs, never
/// store binary data or paths directly. Supports local and cloud (S3/GCS/Azure) storage.
/// Clinical/KVKK-sensitive files are flagged with IsSensitive = true and require
/// explicit consent before access (enforced at the application layer).
/// </summary>
public class FileObject : TenantEntity
{
    /// <summary>'s3', 'gcs', 'azure', or 'local'.</summary>
    public string StorageBackend { get; set; } = "s3";

    public string? Bucket { get; set; }

    /// <summary>Object key / path within the storage backend.</summary>
    public string ObjectKey { get; set; } = string.Empty;

    public string OriginalName { get; set; } = string.Empty;

    public string? MimeType { get; set; }

    public long? ByteSize { get; set; }

    public string? ChecksumSha256 { get; set; }

    /// <summary>True for KVKK special category data (medical reports, etc.).</summary>
    public bool IsSensitive { get; set; }

    /// <summary>'pending', 'clean', 'infected', 'skipped'.</summary>
    public string VirusScanStatus { get; set; } = "pending";

    public Guid? UploadedBy { get; set; }

    public ICollection<FileAttachment> Attachments { get; set; } = [];
}
