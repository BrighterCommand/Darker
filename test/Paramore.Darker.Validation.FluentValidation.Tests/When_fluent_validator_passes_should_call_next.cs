using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker;
using Paramore.Darker.Validation.FluentValidation.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.FluentValidation.Tests;

public class FluentValidationQueryValidatorDecoratorValidQueryTests
{
    [Fact]
    public void When_fluent_validator_passes_should_call_next()
    {
        // Arrange — build a real ServiceProvider with a passing validator for FvTestQuery
        var EXPECTED_RESULT = new FvTestQuery.Result { Value = "success" };
        var query = new FvTestQuery { Name = "Valid Name" };

        var serviceProvider = new ServiceCollection()
            .AddScoped<IValidator<FvTestQuery>, FvTestQueryValidator>()
            .BuildServiceProvider();

        // The decorator is closed over IQuery<TResult> at pipeline runtime (see PipelineBuilder:253)
        var decorator = new FluentValidationQueryValidatorDecorator<IQuery<FvTestQuery.Result>, FvTestQuery.Result>(serviceProvider);

        var nextCallCount = 0;
        FvTestQuery.Result Next(IQuery<FvTestQuery.Result> q)
        {
            nextCallCount++;
            return EXPECTED_RESULT;
        }
        FvTestQuery.Result Fallback(IQuery<FvTestQuery.Result> q) => new FvTestQuery.Result();

        // Act
        var result = decorator.Execute(query, Next, Fallback);

        // Assert — the result is the exact object returned by next, unchanged
        result.ShouldBeSameAs(EXPECTED_RESULT);

        // Assert — next was invoked exactly once
        nextCallCount.ShouldBe(1);
    }
}
