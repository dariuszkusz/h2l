using FluentValidation;

namespace H2l.Application.Users.Commands;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.ExternalId)
            .MaximumLength(200)
            .When(x => x.ExternalId is not null);

        RuleFor(x => x.SystemRole)
            .IsInEnum();

        RuleForEach(x => x.GroupIds)
            .NotEmpty()
            .When(x => x.GroupIds is not null);
    }
}
