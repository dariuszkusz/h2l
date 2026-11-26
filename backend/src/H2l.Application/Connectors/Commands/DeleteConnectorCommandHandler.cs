using H2l.Application.Common;
using H2l.Domain.Audit;
using H2l.Domain.Common;
using H2l.Domain.Connectors;

namespace H2l.Application.Connectors.Commands;

public sealed class DeleteConnectorCommandHandler
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IWorkflowConnectorRepository _connectorRepository;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteConnectorCommandHandler(
        ICurrentUserContext currentUserContext,
        IWorkflowConnectorRepository connectorRepository,
        IAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _currentUserContext = currentUserContext;
        _connectorRepository = connectorRepository;
        _auditLogRepository = auditLogRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(
        DeleteConnectorCommand command,
        CancellationToken cancellationToken)
    {
        var connector = await _connectorRepository.GetByIdForUpdateAsync(command.ConnectorId, cancellationToken);
        if (connector is null)
        {
            return Result.Failure(Error.NotFound("CONNECTOR_NOT_FOUND", "Workflow connector was not found."));
        }

        var now = _dateTimeProvider.UtcNow;
        var deleteResult = connector.SoftDelete(now);
        if (deleteResult.IsFailure)
        {
            return deleteResult;
        }

        var auditResult = AuditLog.Create(
            connector.TenantId,
            _currentUserContext.UserId,
            "CONNECTOR_DELETED",
            "WorkflowConnector",
            connector.Id,
            null,
            now);

        if (auditResult.IsSuccess)
        {
            await _auditLogRepository.AddAsync(auditResult.Value, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
