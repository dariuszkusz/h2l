using H2l.Application.Connectors.DTOs;
using H2l.Application.Connectors.Services;
using H2l.Application.Forms.Schema;
using H2l.Domain.Common;
using H2l.Domain.Workflows;

namespace H2l.Application.Forms.Queries;

public sealed class GetN8nNodesTemplateQueryHandler
{
    private readonly IFormDefinitionRepository _formDefinitionRepository;
    private readonly IFormVersionRepository _formVersionRepository;
    private readonly IFormSchemaValidator _formSchemaValidator;
    private readonly IN8nWorkflowTemplateGenerator _templateGenerator;

    public GetN8nNodesTemplateQueryHandler(
        IFormDefinitionRepository formDefinitionRepository,
        IFormVersionRepository formVersionRepository,
        IFormSchemaValidator formSchemaValidator,
        IN8nWorkflowTemplateGenerator templateGenerator)
    {
        _formDefinitionRepository = formDefinitionRepository;
        _formVersionRepository = formVersionRepository;
        _formSchemaValidator = formSchemaValidator;
        _templateGenerator = templateGenerator;
    }

    public async Task<Result<N8nNodesTemplateResponse>> Handle(
        GetN8nNodesTemplateQuery query,
        CancellationToken cancellationToken)
    {
        var normalizedVariant = string.IsNullOrWhiteSpace(query.Variant)
            ? N8nWorkflowTemplateGenerator.CommunityNodeVariant
            : query.Variant.Trim().ToLowerInvariant();

        if (normalizedVariant != N8nWorkflowTemplateGenerator.CommunityNodeVariant
            && normalizedVariant != N8nWorkflowTemplateGenerator.FallbackWaitNodeVariant)
        {
            return Result.Failure<N8nNodesTemplateResponse>(
                Error.Validation(
                    "N8N_TEMPLATE_VARIANT_INVALID",
                    "Variant must be either 'community-node' or 'fallback-wait-node'."));
        }

        var form = await _formDefinitionRepository.GetByIdAsync(query.FormId, cancellationToken);
        if (form is null)
        {
            return Result.Failure<N8nNodesTemplateResponse>(
                Error.NotFound("FORM_NOT_FOUND", "Form definition was not found."));
        }

        var latestVersion = await _formVersionRepository.GetLatestByFormDefinitionIdAsync(
            form.Id,
            cancellationToken);

        FormSchemaDocument? parsedSchema = null;
        var rawSchema = latestVersion?.SchemaJsonb ?? form.DraftSchemaJsonb;
        if (!string.IsNullOrWhiteSpace(rawSchema))
        {
            var schemaResult = _formSchemaValidator.ParseAndValidate(rawSchema);
            if (schemaResult.IsSuccess)
            {
                parsedSchema = schemaResult.Value;
            }
        }

        var workflowJson = _templateGenerator.Generate(
            form,
            latestVersion,
            parsedSchema,
            normalizedVariant,
            query.ConnectorId);

        return Result.Success(new N8nNodesTemplateResponse(normalizedVariant, workflowJson));
    }
}
