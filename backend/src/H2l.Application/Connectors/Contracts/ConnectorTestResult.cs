namespace H2l.Application.Connectors.Contracts;

public sealed record ConnectorTestResult(
    bool IsReachable,
    string? DetectedEngine,
    string? DetectedVersion,
    long LatencyMs,
    string? ErrorMessage);
