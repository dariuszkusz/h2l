using H2l.Application.Forms.DTOs;
using H2l.Domain.Common;
using H2l.Domain.Workflows;

namespace H2l.Application.Forms.Queries;

public sealed class GetFormAccessRulesQueryHandler
{
    private readonly IFormDefinitionRepository _formDefinitionRepository;
    private readonly IFormAccessRuleRepository _formAccessRuleRepository;

    public GetFormAccessRulesQueryHandler(
        IFormDefinitionRepository formDefinitionRepository,
        IFormAccessRuleRepository formAccessRuleRepository)
    {
        _formDefinitionRepository = formDefinitionRepository;
        _formAccessRuleRepository = formAccessRuleRepository;
    }

    public async Task<Result<FormAccessRulesResponse>> Handle(
        GetFormAccessRulesQuery query,
        CancellationToken cancellationToken)
    {
        var form = await _formDefinitionRepository.GetByIdAsync(query.FormId, cancellationToken);
        if (form is null)
        {
            return Result.Failure<FormAccessRulesResponse>(
                Error.NotFound("FORM_DEFINITION_NOT_FOUND", "Form definition was not found."));
        }

        var rules = await _formAccessRuleRepository.ListByFormDefinitionIdAsync(query.FormId, cancellationToken);
        var items = rules
            .Select(r => new FormAccessRuleItem(r.Id, r.GroupId, r.UserId, r.AccessScope))
            .ToList();

        return Result.Success(new FormAccessRulesResponse(query.FormId, items));
    }
}
