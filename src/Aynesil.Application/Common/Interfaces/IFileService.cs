namespace Aynesil.Application.Common.Interfaces;

public record FileUploadRequest(
    Stream Content,
    string FileName,
    string ContentType,
    bool IsSensitive = false,
    string? Purpose = null);

public record FileUploadResult(
    Guid FileId,
    string ObjectKey,
    string OriginalName,
    long ByteSize,
    string? PublicUrl);

/// <summary>
/// File management abstraction. Files are stored via the configured IStorageProvider.
/// Metadata is persisted in core.file_object. Access control is enforced by the application layer.
/// Sensitive files (KVKK special category) require consent before access is granted.
/// </summary>
public interface IFileService
{
    Task<FileUploadResult> UploadAsync(
        FileUploadRequest request,
        Guid? ownerId = null,
        string? ownerSchema = null,
        string? ownerTable = null,
        CancellationToken ct = default);

    Task<Stream> DownloadAsync(Guid fileId, CancellationToken ct = default);

    Task<string> GetPresignedUrlAsync(Guid fileId, TimeSpan expiry, CancellationToken ct = default);

    Task DeleteAsync(Guid fileId, CancellationToken ct = default);
}

/// <summary>Storage backend abstraction. Swap local ↔ S3 without changing business logic.</summary>
public interface IStorageProvider
{
    Task<(string bucket, string key)> UploadAsync(Stream content, string objectKey, string contentType, CancellationToken ct);
    Task<Stream> DownloadAsync(string bucket, string objectKey, CancellationToken ct);
    Task<string> GetPresignedUrlAsync(string bucket, string objectKey, TimeSpan expiry, CancellationToken ct);
    Task DeleteAsync(string bucket, string objectKey, CancellationToken ct);
}
