namespace H2l.Application.Setup.Commands;

public sealed record InitializeSetupResponse(
    Guid InstanceId,
    Guid AdminUserId);
