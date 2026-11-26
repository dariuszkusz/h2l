namespace H2l.Application.Common;

public interface IAttachmentStorageService
{
    long MaxFileSizeBytes { get; }

    IReadOnlySet<string> DefaultAllowedMimeTypes { get; }

    Task<StoredAttachmentResult> SaveEncryptedAsync(
        Guid tenantId,
        Guid attachmentId,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default);

    Task<byte[]> ReadDecryptedAsync(
        Guid tenantId,
        Guid attachmentId,
        string storagePath,
        CancellationToken cancellationToken = default);
}
