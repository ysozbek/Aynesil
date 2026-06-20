using System.Security.Cryptography;
using Aynesil.Application.Common.Interfaces;
using Aynesil.Domain.Modules.Core.Entities;
using Aynesil.Infrastructure.Persistence;

namespace Aynesil.Infrastructure.Services.Files;

public sealed class FileService : IFileService
{
    private readonly AynesilDbContext _db;
    private readonly IStorageProvider _storage;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUser;

    public FileService(AynesilDbContext db, IStorageProvider storage, ITenantContext tenantContext, ICurrentUserService currentUser)
    {
        _db = db;
        _storage = storage;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
    }

    public async Task<FileUploadResult> UploadAsync(FileUploadRequest req, Guid? ownerId = null, string? ownerSchema = null, string? ownerTable = null, CancellationToken ct = default)
    {
        var corporationId = _tenantContext.CorporationId
            ?? throw new InvalidOperationException("Tenant context required for file uploads.");

        var extension = Path.GetExtension(req.FileName);
        var objectKey = $"{corporationId}/{Guid.CreateVersion7():N}{extension}";

        // Compute checksum while uploading
        using var sha = SHA256.Create();
        await using var hashStream = new CryptoStream(req.Content, sha, CryptoStreamMode.Read, leaveOpen: true);

        var (bucket, key) = await _storage.UploadAsync(hashStream, objectKey, req.ContentType, ct);
        var checksum = Convert.ToHexString(sha.Hash ?? []).ToLowerInvariant();

        var fileObject = new FileObject
        {
            CorporationId = corporationId,
            StorageBackend = "local",
            Bucket = bucket == "local" ? null : bucket,
            ObjectKey = key,
            OriginalName = req.FileName,
            MimeType = req.ContentType,
            IsSensitive = req.IsSensitive,
            ChecksumSha256 = checksum,
            UploadedBy = _currentUser.UserId
        };
        _db.FileObjects.Add(fileObject);

        if (ownerId.HasValue && ownerSchema is not null && ownerTable is not null)
        {
            _db.FileAttachments.Add(new FileAttachment
            {
                CorporationId = corporationId,
                FileId = fileObject.Id,
                OwnerSchema = ownerSchema,
                OwnerTable = ownerTable,
                OwnerId = ownerId.Value,
                Purpose = req.Purpose,
                CreatedBy = _currentUser.UserId
            });
        }

        await _db.SaveChangesAsync(ct);

        return new FileUploadResult(fileObject.Id, key, req.FileName, fileObject.ByteSize ?? 0, null);
    }

    public async Task<Stream> DownloadAsync(Guid fileId, CancellationToken ct = default)
    {
        var file = await _db.FileObjects.FindAsync([fileId], ct)
            ?? throw new FileNotFoundException($"File {fileId} not found.");
        return await _storage.DownloadAsync(file.Bucket ?? "local", file.ObjectKey, ct);
    }

    public async Task<string> GetPresignedUrlAsync(Guid fileId, TimeSpan expiry, CancellationToken ct = default)
    {
        var file = await _db.FileObjects.FindAsync([fileId], ct)
            ?? throw new FileNotFoundException($"File {fileId} not found.");
        return await _storage.GetPresignedUrlAsync(file.Bucket ?? "local", file.ObjectKey, expiry, ct);
    }

    public async Task DeleteAsync(Guid fileId, CancellationToken ct = default)
    {
        var file = await _db.FileObjects.FindAsync([fileId], ct)
            ?? throw new FileNotFoundException($"File {fileId} not found.");
        await _storage.DeleteAsync(file.Bucket ?? "local", file.ObjectKey, ct);
        file.DeletedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
