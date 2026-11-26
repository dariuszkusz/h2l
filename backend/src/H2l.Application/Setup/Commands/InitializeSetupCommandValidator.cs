using FluentValidation;

namespace H2l.Application.Setup.Commands;

public sealed class InitializeSetupCommandValidator : AbstractValidator<InitializeSetupCommand>
{
    private static readonly string[] SupportedLanguages = ["en", "de", "pl"];
    private static readonly string[] SupportedPalettes = ["slate", "ocean", "forest"];

    public InitializeSetupCommandValidator()
    {
        RuleFor(x => x.AdminEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(x => x.AdminPassword)
            .NotEmpty()
            .MinimumLength(12)
            .MaximumLength(128);

        RuleFor(x => x.DefaultLanguage)
            .NotEmpty()
            .Must(lang => SupportedLanguages.Contains(lang.Trim().ToLowerInvariant()))
            .WithMessage("DefaultLanguage must be one of: en, de, pl.");

        RuleFor(x => x.SelectedPalette)
            .NotEmpty()
            .Must(palette => SupportedPalettes.Contains(palette.Trim().ToLowerInvariant()))
            .WithMessage("SelectedPalette must be one of: slate, ocean, forest.");

        When(x => x.Smtp is not null, () =>
        {
            RuleFor(x => x.Smtp!.Host)
                .NotEmpty()
                .MaximumLength(255);

            RuleFor(x => x.Smtp!.Port)
                .InclusiveBetween(1, 65535);

            RuleFor(x => x.Smtp!.Username)
                .MaximumLength(255)
                .When(x => x.Smtp!.Username is not null);

            RuleFor(x => x.Smtp!.Password)
                .MaximumLength(512)
                .When(x => x.Smtp!.Password is not null);

            RuleFor(x => x.Smtp!.FromAddress)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(320);

            RuleFor(x => x.Smtp!.FromName)
                .MaximumLength(200)
                .When(x => x.Smtp!.FromName is not null);
        });
    }
}
