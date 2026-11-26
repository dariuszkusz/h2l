namespace H2l.Application.Connectors.DTOs;

public sealed record ConnectorListResponse(
    IReadOnlyList<ConnectorSummaryResponse> Items);
