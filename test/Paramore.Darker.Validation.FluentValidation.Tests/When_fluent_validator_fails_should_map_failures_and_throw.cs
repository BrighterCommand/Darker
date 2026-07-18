using System.Linq;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker;
using Paramore.Darker.Validation;
using Paramore.Darker.Validation.FluentValidation.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.FluentValidation.Tests;

public class FluentValidationQueryValidatorDecoratorInvalidQueryTests
{
    [Fact]
    public void When_fluent_validator_fails_should_map_failures_and_throw()
    {
        // Arrange — a query that violates two distinct rules: Name is empty, Age is negative
        var INVALID_NAME = string.Empty;
        const int INVALID_AGE = -1;
        var query = new FvTestQuery { Name = INVALID_NAME, Age = INVALID_AGE };

        var serviceProvider = new ServiceCollection()
            .AddScoped<IValidator<FvTestQuery>, FvTestQueryValidator>()
            .BuildServiceProvider();

        // The decorator is closed over IQuery<TResult> at pipeline runtime (see PipelineBuilder:253)
        var decorator = new FluentValidationQueryValidatorDecorator<IQuery<FvTestQuery.Result>, FvTestQuery.Result>(serviceProvider);

        var nextCallCount = 0;
        FvTestQuery.Result Next(IQuery<FvTestQuery.Result> q)
        {
            nextCallCount++;
            return new FvTestQuery.Result();
        }
        FvTestQuery.Result Fallback(IQuery<FvTestQuery.Result> q) => new FvTestQuery.Result();

        // Act
        var exception = Should.Throw<QueryValidationException>(
            () => decorator.Execute(query, Next, Fallback));

        // Assert — two failures are surfaced (FR6: the whole collection, not just the first)
        exception.Errors.Count.ShouldBe(2);

        var errors = exception.Errors.ToList();
        var propertyNames = errors.Select(e => e.PropertyName).ToList();
        propertyNames.ShouldContain("Name");
        propertyNames.ShouldContain("Age");

        // Assert — all four fields are mapped for at least the Name failure
        var nameError = errors.Single(e => e.PropertyName == "Name");
        nameError.PropertyName.ShouldBe("Name");
        nameError.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        nameError.AttemptedValue.ShouldBe(INVALID_NAME);
        nameError.ErrorCode.ShouldNotBeNullOrWhiteSpace();

        // Assert — next was never invoked (pipeline short-circuits on failure)
        nextCallCount.ShouldBe(0);
    }
}
