using System;
using Paramore.Darker.Validation;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.Tests;

public class ValidateQueryAttributeAsyncTests
{
    [Fact]
    public void When_validate_query_attribute_async_created_should_return_abstract_async_decorator_type()
    {
        // Arrange — construct with an explicit step number; this is the only configurable param
        const int STEP = 5;

        // Act
        var attribute = new ValidateQueryAttributeAsync(STEP);

        // Assert — GetDecoratorType() returns the abstract open generic async decorator, not any provider type
        attribute.GetDecoratorType().ShouldBe(typeof(ValidateQueryDecoratorAsync<,>));

        // Assert — GetAttributeParams() returns an empty array (no per-attribute state beyond Step)
        attribute.GetAttributeParams().ShouldBeEmpty();

        // Assert — Step is preserved from the constructor
        attribute.Step.ShouldBe(STEP);
    }
}
