using FluentValidation;

namespace H2l.Application.Tasks.Commands;

public sealed class AssignTaskCommandValidator : AbstractValidator<AssignTaskCommand>
{
    public AssignTaskCommandValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
    }
}
