using System;
using System.Collections.Generic;
using Paramore.Darker.Validation.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.Tests;

public class ValidateQueryDecoratorValidQueryTests
{
    [Fact]
    public void When_query_is_valid_should_call_next()
    {
        // Arrange — an empty error list signals that the query is valid; track invocations via counter
        var EXPECTED_RESULT = new ValidationTestQuery.Result { Value = "hello" };
        var query = new ValidationTestQuery();
        var decorator = new StubValidateQueryDecorator<ValidationTestQuery, ValidationTestQuery.Result>(
            new List<QueryValidationError>());

        var nextCallCount = 0;
        ValidationTestQuery.Result Next(ValidationTestQuery q)
        {
            nextCallCount++;
            return EXPECTED_RESULT;
        }
        ValidationTestQuery.Result Fallback(ValidationTestQuery q) => new ValidationTestQuery.Result();

        // Act
        var result = decorator.Execute(query, Next, Fallback);

        // Assert — the result is the exact object returned by next, unchanged
        result.ShouldBeSameAs(EXPECTED_RESULT);

        // Assert — next was invoked exactly once
        nextCallCount.ShouldBe(1);
    }
}
