namespace H2l.Application.Common;

public sealed record StoredAttachmentResult(
    string StoragePath,
    bool IsEncrypted);
