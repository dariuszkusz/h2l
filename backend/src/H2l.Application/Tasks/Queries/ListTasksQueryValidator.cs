using FluentValidation;

namespace H2l.Application.Tasks.Queries;

public sealed class ListTasksQueryValidator : AbstractValidator<ListTasksQuery>
{
    public ListTasksQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}
