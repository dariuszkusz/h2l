namespace H2l.Application.Tasks.DTOs;

public sealed record AttachmentUploadResponse(
    Guid Id,
    string FieldVariableKey,
    string OriginalFileName,
    string ContentType,
    long SizeBytes);
