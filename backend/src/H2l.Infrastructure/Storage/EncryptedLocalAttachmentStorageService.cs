using H2l.Application.Common;
using H2l.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace H2l.Infrastructure.Storage;

public sealed class EncryptedLocalAttachmentStorageService : IAttachmentStorageService
{
    private readonly string _rootDirectory;
    private readonly IAesGcmEncryptionService _encryptionService;

    public long MaxFileSizeBytes { get; }

    public IReadOnlySet<string> DefaultAllowedMimeTypes { get; }

    public EncryptedLocalAttachmentStorageService(
        IOptions<AttachmentsOptions> options,
        IAesGcmEncryptionService encryptionService)
    {
        ArgumentNullException.ThrowIfNull(options);
        _encryptionService = encryptionService;

        var value = options.Value;
        MaxFileSizeBytes = (long)value.MaxSizeMb * 1024L * 1024L;
        DefaultAllowedMimeTypes = new HashSet<string>(value.AllowedMimeTypes, StringComparer.OrdinalIgnoreCase);
        _rootDirectory = Path.GetFullPath(value.StoragePath);
    }

    public async Task<StoredAttachmentResult> SaveEncryptedAsync(
        Guid tenantId,
        Guid attachmentId,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        var relativePath = Path.Combine(tenantId.ToString("N"), $"{attachmentId:N}.enc");
        var fullPath = ResolveSafePath(relativePath);

        var parentDir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(parentDir))
        {
            Directory.CreateDirectory(parentDir);
        }

        var encryptedBytes = _encryptionService.EncryptBytes(
            content.Span,
            $"attachment:{tenantId:N}:{attachmentId:N}");

        await File.WriteAllBytesAsync(fullPath, encryptedBytes, cancellationToken);

        return new StoredAttachmentResult(relativePath.Replace('\\', '/'), IsEncrypted: true);
    }

    public async Task<byte[]> ReadDecryptedAsync(
        Guid tenantId,
        Guid attachmentId,
        string storagePath,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafePath(storagePath);
        var encryptedBytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
        return _encryptionService.DecryptBytes(
            encryptedBytes,
            $"attachment:{tenantId:N}:{attachmentId:N}");
    }

    private string ResolveSafePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new InvalidOperationException("Attachment storage path must be relative.");
        }

        var combined = Path.GetFullPath(Path.Combine(_rootDirectory, relativePath));
        var normalizedRoot = _rootDirectory.EndsWith(Path.DirectorySeparatorChar)
            ? _rootDirectory
            : _rootDirectory + Path.DirectorySeparatorChar;

        if (!combined.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Attempted path traversal outside attachment storage root.");
        }

        return combined;
    }
}
