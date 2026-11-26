using System.Text.Json;
using FluentValidation;

namespace H2l.Application.Forms.Commands;

public sealed class CreateFormDefinitionCommandValidator : AbstractValidator<CreateFormDefinitionCommand>
{
    public CreateFormDefinitionCommandValidator()
    {
        RuleFor(x => x.ProcessId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.StepKey)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.DraftSchemaJsonb)
            .Must(json => !json.HasValue || json.Value.ValueKind == JsonValueKind.Object || json.Value.ValueKind == JsonValueKind.Null)
            .WithMessage("DraftSchemaJsonb must be a JSON object when provided.");
    }
}
