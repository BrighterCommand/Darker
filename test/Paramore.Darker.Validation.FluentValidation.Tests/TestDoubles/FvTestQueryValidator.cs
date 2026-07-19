using FluentValidation;

namespace Paramore.Darker.Validation.FluentValidation.Tests.TestDoubles;

internal sealed class FvTestQueryValidator : AbstractValidator<FvTestQuery>
{
    public FvTestQueryValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Age).GreaterThan(0);
    }
}
