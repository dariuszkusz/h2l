using FluentValidation;

namespace H2l.Application.Connectors.Commands;

public sealed class CreateConnectorCommandValidator : AbstractValidator<CreateConnectorCommand>
{
    public CreateConnectorCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .MaximumLength(2048);

        RuleFor(x => x.CallbackUrl)
            .MaximumLength(2048)
            .When(x => !string.IsNullOrWhiteSpace(x.CallbackUrl));

        RuleFor(x => x.CustomCaCertificatePem)
            .MaximumLength(65536)
            .When(x => !string.IsNullOrWhiteSpace(x.CustomCaCertificatePem));
    }
}
