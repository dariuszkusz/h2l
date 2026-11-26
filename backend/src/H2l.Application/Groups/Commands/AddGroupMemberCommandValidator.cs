using FluentValidation;

namespace H2l.Application.Groups.Commands;

public sealed class AddGroupMemberCommandValidator : AbstractValidator<AddGroupMemberCommand>
{
    public AddGroupMemberCommandValidator()
    {
        RuleFor(x => x.GroupId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
    }
}
