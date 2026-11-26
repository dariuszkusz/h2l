using System.Text.Json;
using H2l.Application.Forms.DTOs;
using H2l.Domain.Common;
using H2l.Domain.Workflows;

namespace H2l.Application.Forms.Queries;

public sealed class ListProcessFormsQueryHandler
{
    private readonly IWorkflowProcessRepository _workflowProcessRepository;
    private readonly IFormDefinitionRepository _formDefinitionRepository;

    public ListProcessFormsQueryHandler(
        IWorkflowProcessRepository workflowProcessRepository,
        IFormDefinitionRepository formDefinitionRepository)
    {
        _workflowProcessRepository = workflowProcessRepository;
        _formDefinitionRepository = formDefinitionRepository;
    }

    public async Task<Result<IReadOnlyList<FormDefinitionResponse>>> Handle(
        ListProcessFormsQuery query,
        CancellationToken cancellationToken)
    {
        var process = await _workflowProcessRepository.GetByIdAsync(query.ProcessId, cancellationToken);
        if (process is null)
        {
            return Result.Failure<IReadOnlyList<FormDefinitionResponse>>(
                Error.NotFound("WORKFLOW_PROCESS_NOT_FOUND", "Workflow process was not found."));
        }

        var forms = await _formDefinitionRepository.ListByProcessIdAsync(query.ProcessId, cancellationToken);
        var items = forms
            .Select(f => new FormDefinitionResponse(
                f.Id,
                f.WorkflowProcessId,
                f.Name,
                f.StepKey,
                f.Status,
                ParseOptionalJson(f.DraftSchemaJsonb),
                f.UpdatedAt))
            .ToList();

        return Result.Success<IReadOnlyList<FormDefinitionResponse>>(items);
    }

    private static JsonElement? ParseOptionalJson(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
        {
            return null;
        }

        using var document = JsonDocument.Parse(rawJson);
        return document.RootElement.Clone();
    }
}
