using FluentValidation;

namespace H2l.Application.Projects.Commands;

public sealed class CreateWorkflowProcessCommandValidator : AbstractValidator<CreateWorkflowProcessCommand>
{
    public CreateWorkflowProcessCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.ExternalProcessKey)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.ConnectorId)
            .Must(id => id is null || id.Value != Guid.Empty)
            .WithMessage("ConnectorId cannot be an empty GUID when specified.");

        RuleFor(x => x.Status)
            .IsInEnum()
            .When(x => x.Status.HasValue);
    }
}
