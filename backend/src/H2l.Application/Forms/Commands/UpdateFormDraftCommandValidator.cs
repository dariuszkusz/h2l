using System.Text.Json;
using FluentValidation;

namespace H2l.Application.Forms.Commands;

public sealed class UpdateFormDraftCommandValidator : AbstractValidator<UpdateFormDraftCommand>
{
    public UpdateFormDraftCommandValidator()
    {
        RuleFor(x => x.FormId)
            .NotEmpty();

        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name!)
                .NotEmpty()
                .MaximumLength(200);
        });

        RuleFor(x => x.DraftSchema)
            .Must(el => el.ValueKind == JsonValueKind.Object)
            .WithMessage("Draft schema must be a valid JSON object.");
    }
}
