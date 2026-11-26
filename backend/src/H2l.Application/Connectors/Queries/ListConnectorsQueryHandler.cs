using H2l.Application.Connectors.DTOs;
using H2l.Domain.Common;
using H2l.Domain.Connectors;

namespace H2l.Application.Connectors.Queries;

public sealed class ListConnectorsQueryHandler
{
    private readonly IWorkflowConnectorRepository _connectorRepository;

    public ListConnectorsQueryHandler(IWorkflowConnectorRepository connectorRepository)
    {
        _connectorRepository = connectorRepository;
    }

    public async Task<Result<ConnectorListResponse>> Handle(
        ListConnectorsQuery query,
        CancellationToken cancellationToken)
    {
        var connectors = await _connectorRepository.ListAsync(cancellationToken);
        var items = connectors
            .Select(c => new ConnectorSummaryResponse(
                c.Id,
                c.Name,
                c.ConnectorType,
                c.BaseUrl,
                c.AuthType,
                c.HasCredentials,
                c.HasCustomCaCertificate,
                c.CallbackUrl,
                c.IsActive,
                c.CreatedAt))
            .ToList();

        return Result.Success(new ConnectorListResponse(items));
    }
}
