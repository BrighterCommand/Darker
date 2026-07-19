using System.Collections.Generic;
using Paramore.Darker.Validation.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.Tests;

public class ValidateQueryDecoratorInvalidQueryTests
{
    [Fact]
    public void When_query_is_invalid_should_throw_and_not_call_next()
    {
        // Arrange — two errors to verify the full collection is carried through to the exception
        var EXPECTED_ERROR_ONE = new QueryValidationError("Name", "must not be empty", "", "NotEmpty");
        var EXPECTED_ERROR_TWO = new QueryValidationError("Age", "must be greater than zero", -1, "GreaterThan");
        var errors = new List<QueryValidationError> { EXPECTED_ERROR_ONE, EXPECTED_ERROR_TWO };

        var query = new ValidationTestQuery();
        var decorator = new StubValidateQueryDecorator<ValidationTestQuery, ValidationTestQuery.Result>(errors);

        var nextCallCount = 0;
        ValidationTestQuery.Result Next(ValidationTestQuery q)
        {
            nextCallCount++;
            return new ValidationTestQuery.Result();
        }
        ValidationTestQuery.Result Fallback(ValidationTestQuery q) => new ValidationTestQuery.Result();

        // Act — invoke Execute on a decorator configured to return validation errors
        var exception = Should.Throw<QueryValidationException>(
            () => decorator.Execute(query, Next, Fallback));

        // Assert — the exception carries exactly the errors returned by Validate
        exception.Errors.Count.ShouldBe(2);
        exception.Errors.ShouldContain(EXPECTED_ERROR_ONE);
        exception.Errors.ShouldContain(EXPECTED_ERROR_TWO);

        // Assert — next was never invoked (pipeline short-circuits on validation failure)
        nextCallCount.ShouldBe(0);
    }
}
