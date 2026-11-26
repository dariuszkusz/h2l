using FluentValidation;

namespace H2l.Application.Users.Commands;

public sealed class UpdateUserPreferencesCommandValidator : AbstractValidator<UpdateUserPreferencesCommand>
{
    private static readonly string[] SupportedLanguages = ["en", "de", "pl"];
    private static readonly string[] SupportedThemes = ["light", "dark", "high-contrast"];

    public UpdateUserPreferencesCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.PreferredLanguage is not null || x.PreferredTheme is not null)
            .WithMessage("At least one preference (preferredLanguage or preferredTheme) must be provided.");

        RuleFor(x => x.PreferredLanguage)
            .Must(lang => lang is null || SupportedLanguages.Contains(lang.Trim().ToLowerInvariant()))
            .WithMessage("PreferredLanguage must be one of: en, de, pl.");

        RuleFor(x => x.PreferredTheme)
            .Must(theme => theme is null || SupportedThemes.Contains(theme.Trim().ToLowerInvariant()))
            .WithMessage("PreferredTheme must be one of: light, dark, high-contrast.");
    }
}
