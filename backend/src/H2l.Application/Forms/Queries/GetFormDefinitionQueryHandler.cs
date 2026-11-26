using System.Text.Json;
using H2l.Application.Forms.DTOs;
using H2l.Domain.Common;
using H2l.Domain.Workflows;

namespace H2l.Application.Forms.Queries;

public sealed class GetFormDefinitionQueryHandler
{
    private readonly IFormDefinitionRepository _formDefinitionRepository;
    private readonly IFormVersionRepository _formVersionRepository;

    public GetFormDefinitionQueryHandler(
        IFormDefinitionRepository formDefinitionRepository,
        IFormVersionRepository formVersionRepository)
    {
        _formDefinitionRepository = formDefinitionRepository;
        _formVersionRepository = formVersionRepository;
    }

    public async Task<Result<FormDefinitionDetailResponse>> Handle(
        GetFormDefinitionQuery query,
        CancellationToken cancellationToken)
    {
        var form = await _formDefinitionRepository.GetByIdAsync(query.FormId, cancellationToken);
        if (form is null)
        {
            return Result.Failure<FormDefinitionDetailResponse>(
                Error.NotFound("FORM_DEFINITION_NOT_FOUND", "Form definition was not found."));
        }

        var latestVersionNumber = await _formVersionRepository.GetLatestVersionNumberAsync(form.Id, cancellationToken);

        JsonElement? draftSchema = null;
        if (!string.IsNullOrWhiteSpace(form.DraftSchemaJsonb))
        {
            using var doc = JsonDocument.Parse(form.DraftSchemaJsonb);
            draftSchema = doc.RootElement.Clone();
        }

        return Result.Success(new FormDefinitionDetailResponse(
            form.Id,
            form.WorkflowProcessId,
            form.Name,
            form.StepKey,
            form.Status,
            draftSchema,
            latestVersionNumber,
            form.UpdatedAt));
    }
}
