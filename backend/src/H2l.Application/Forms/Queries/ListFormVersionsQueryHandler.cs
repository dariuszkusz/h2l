using System.Text.Json;
using H2l.Application.Forms.DTOs;
using H2l.Domain.Common;
using H2l.Domain.Workflows;

namespace H2l.Application.Forms.Queries;

public sealed class ListFormVersionsQueryHandler
{
    private readonly IFormDefinitionRepository _formDefinitionRepository;
    private readonly IFormVersionRepository _formVersionRepository;

    public ListFormVersionsQueryHandler(
        IFormDefinitionRepository formDefinitionRepository,
        IFormVersionRepository formVersionRepository)
    {
        _formDefinitionRepository = formDefinitionRepository;
        _formVersionRepository = formVersionRepository;
    }

    public async Task<Result<IReadOnlyList<FormVersionSummaryResponse>>> Handle(
        ListFormVersionsQuery query,
        CancellationToken cancellationToken)
    {
        var form = await _formDefinitionRepository.GetByIdAsync(query.FormId, cancellationToken);
        if (form is null)
        {
            return Result.Failure<IReadOnlyList<FormVersionSummaryResponse>>(
                Error.NotFound("FORM_DEFINITION_NOT_FOUND", "Form definition was not found."));
        }

        var versions = await _formVersionRepository.ListByFormDefinitionIdAsync(form.Id, cancellationToken);
        var items = new List<FormVersionSummaryResponse>(versions.Count);

        foreach (var version in versions)
        {
            using var doc = JsonDocument.Parse(version.SchemaJsonb);
            items.Add(new FormVersionSummaryResponse(
                version.Id,
                version.VersionNumber,
                version.PublishedAt,
                version.PublishedByUserId,
                doc.RootElement.Clone()));
        }

        return Result.Success<IReadOnlyList<FormVersionSummaryResponse>>(items);
    }
}
