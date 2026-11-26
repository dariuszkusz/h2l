using FluentValidation;

namespace H2l.Application.Licensing.Commands;

public sealed class ActivateLicenseCommandValidator : AbstractValidator<ActivateLicenseCommand>
{
    public ActivateLicenseCommandValidator()
    {
        RuleFor(x => x.LicenseKey)
            .NotEmpty()
            .MaximumLength(8192);
    }
}
