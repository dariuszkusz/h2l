using FluentValidation;

namespace H2l.Application.Tasks.Commands;

public sealed class SubmitTaskCommandValidator : AbstractValidator<SubmitTaskCommand>
{
    public SubmitTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty();

        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Values)
            .NotNull();
    }
}
