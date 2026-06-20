using Aynesil.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Aynesil.Infrastructure.Services.Files;

/// <summary>
/// Local filesystem storage provider for development and single-server deployments.
/// Files are stored under the configured root path. Presigned URLs are generated
/// via a server-side proxy endpoint rather than direct S3-style signed URLs.
/// Replace with S3StorageProvider for production cloud deployments.
/// </summary>
public sealed class LocalStorageProvider : IStorageProvider
{
    private readonly string _rootPath;
    private readonly string _baseUrl;

    public LocalStorageProvider(IConfiguration config)
    {
        _rootPath = config["Storage:Local:RootPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        _baseUrl = config["Storage:Local:BaseUrl"] ?? "http://localhost:5000/files";
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<(string bucket, string key)> UploadAsync(
        Stream content, string objectKey, string contentType, CancellationToken ct)
    {
        var fullPath = Path.Combine(_rootPath, objectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);
        return ("local", objectKey);
    }

    public Task<Stream> DownloadAsync(string bucket, string objectKey, CancellationToken ct)
    {
        var fullPath = Path.Combine(_rootPath, objectKey);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"File not found: {objectKey}");
        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public Task<string> GetPresignedUrlAsync(string bucket, string objectKey, TimeSpan expiry, CancellationToken ct)
    {
        // Local: return a simple URL — access is controlled by the API endpoint
        return Task.FromResult($"{_baseUrl}/{Uri.EscapeDataString(objectKey)}");
    }

    public Task DeleteAsync(string bucket, string objectKey, CancellationToken ct)
    {
        var fullPath = Path.Combine(_rootPath, objectKey);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }
}
