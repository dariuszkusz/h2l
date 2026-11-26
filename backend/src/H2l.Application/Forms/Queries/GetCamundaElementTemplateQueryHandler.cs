using System.Text.Json;
using H2l.Application.Connectors.Services;
using H2l.Application.Forms.Schema;
using H2l.Domain.Common;
using H2l.Domain.Workflows;

namespace H2l.Application.Forms.Queries;

public sealed class GetCamundaElementTemplateQueryHandler
{
    private readonly IFormDefinitionRepository _formDefinitionRepository;
    private readonly IFormVersionRepository _formVersionRepository;
    private readonly IFormSchemaValidator _formSchemaValidator;
    private readonly ICamundaElementTemplateGenerator _templateGenerator;

    public GetCamundaElementTemplateQueryHandler(
        IFormDefinitionRepository formDefinitionRepository,
        IFormVersionRepository formVersionRepository,
        IFormSchemaValidator formSchemaValidator,
        ICamundaElementTemplateGenerator templateGenerator)
    {
        _formDefinitionRepository = formDefinitionRepository;
        _formVersionRepository = formVersionRepository;
        _formSchemaValidator = formSchemaValidator;
        _templateGenerator = templateGenerator;
    }

    public async Task<Result<JsonElement>> Handle(
        GetCamundaElementTemplateQuery query,
        CancellationToken cancellationToken)
    {
        var form = await _formDefinitionRepository.GetByIdAsync(query.FormId, cancellationToken);
        if (form is null)
        {
            return Result.Failure<JsonElement>(
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

        var template = _templateGenerator.Generate(form, latestVersion, parsedSchema, query.Generic);
        return Result.Success(template);
    }
}
