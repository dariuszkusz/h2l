namespace H2l.Application.Connectors.DTOs;

public sealed record TestConnectorConnectionResponse(
    bool IsReachable,
    string? DetectedEngine,
    string? DetectedVersion,
    long LatencyMs,
    string? ErrorMessage);
