using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Validation.FluentValidation.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.FluentValidation.Tests;

public class FluentValidationQueryValidatorDecoratorMissingValidatorTests
{
    [Fact]
    public void When_no_fluent_validator_registered_should_throw_configuration_exception()
    {
        // Arrange — an empty ServiceProvider with no validators registered
        var query = new FvTestQuery { Name = "Any Name" };

        var serviceProvider = new ServiceCollection()
            .BuildServiceProvider();

        // The decorator is closed over IQuery<TResult> at pipeline runtime (see PipelineBuilder:253)
        var decorator = new FluentValidationQueryValidatorDecorator<IQuery<FvTestQuery.Result>, FvTestQuery.Result>(serviceProvider);

        FvTestQuery.Result Next(IQuery<FvTestQuery.Result> q) => new FvTestQuery.Result();
        FvTestQuery.Result Fallback(IQuery<FvTestQuery.Result> q) => new FvTestQuery.Result();

        // Act
        var exception = Should.Throw<ConfigurationException>(
            () => decorator.Execute(query, Next, Fallback));

        // Assert — the message names the runtime query type so the developer knows what is missing
        exception.Message.ShouldContain(nameof(FvTestQuery));
    }
}
