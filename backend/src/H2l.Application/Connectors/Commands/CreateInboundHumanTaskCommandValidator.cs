using FluentValidation;

namespace H2l.Application.Connectors.Commands;

public sealed class CreateInboundHumanTaskCommandValidator : AbstractValidator<CreateInboundHumanTaskCommand>
{
    public CreateInboundHumanTaskCommandValidator()
    {
        RuleFor(x => x.ConnectorId)
            .NotEmpty();

        RuleFor(x => x.ExternalTaskId)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.FormId)
            .NotEmpty();

        RuleFor(x => x.FormVersion)
            .GreaterThanOrEqualTo(1)
            .When(x => x.FormVersion.HasValue);

        RuleFor(x => x.Assignee)
            .MaximumLength(320)
            .When(x => !string.IsNullOrWhiteSpace(x.Assignee));

        RuleFor(x => x.ResumeUrl)
            .MaximumLength(2048)
            .When(x => !string.IsNullOrWhiteSpace(x.ResumeUrl));

        RuleFor(x => x.TimeoutIso8601)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.TimeoutIso8601));
    }
}
