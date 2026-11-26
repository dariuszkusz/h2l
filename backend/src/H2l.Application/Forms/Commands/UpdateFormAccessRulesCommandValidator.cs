using FluentValidation;

namespace H2l.Application.Forms.Commands;

public sealed class UpdateFormAccessRulesCommandValidator : AbstractValidator<UpdateFormAccessRulesCommand>
{
    public UpdateFormAccessRulesCommandValidator()
    {
        RuleFor(x => x.FormId).NotEmpty();

        RuleFor(x => x.Rules).NotNull();

        RuleForEach(x => x.Rules).ChildRules(rule =>
        {
            rule.RuleFor(r => r)
                .Must(r => r.GroupId.HasValue || r.UserId.HasValue)
                .WithMessage("Each access rule must specify at least groupId or userId.");

            rule.RuleFor(r => r.GroupId)
                .Must(id => !id.HasValue || id.Value != Guid.Empty)
                .WithMessage("GroupId cannot be an empty GUID.");

            rule.RuleFor(r => r.UserId)
                .Must(id => !id.HasValue || id.Value != Guid.Empty)
                .WithMessage("UserId cannot be an empty GUID.");
        });
    }
}
