using FluentValidation;

namespace H2l.Application.Connectors.Commands;

public sealed class UpdateConnectorCommandValidator : AbstractValidator<UpdateConnectorCommand>
{
    public UpdateConnectorCommandValidator()
    {
        RuleFor(x => x.ConnectorId)
            .NotEmpty();

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
