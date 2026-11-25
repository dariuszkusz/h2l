namespace H2l.Application.Setup.Queries;

public sealed record SetupStatusResponse(bool IsInitialized, Guid InstanceId);
