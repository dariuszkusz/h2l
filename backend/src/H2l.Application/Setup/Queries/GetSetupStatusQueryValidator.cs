using FluentValidation;

namespace H2l.Application.Setup.Queries;

public sealed class GetSetupStatusQueryValidator : AbstractValidator<GetSetupStatusQuery>
{
    public GetSetupStatusQueryValidator()
    {
    }
}
