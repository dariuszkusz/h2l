namespace H2l.Application.Tasks.Commands;

public sealed record UploadTaskAttachmentCommand(
    Guid TaskId,
    string FieldVariableKey,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    byte[] Content);
