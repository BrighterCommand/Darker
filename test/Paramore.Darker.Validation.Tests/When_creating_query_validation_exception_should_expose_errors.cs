using System;
using System.Collections.Generic;
using Paramore.Darker.Validation;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.Tests;

public class QueryValidationExceptionTests
{
    [Fact]
    public void When_creating_query_validation_exception_should_expose_errors()
    {
        // Arrange — two distinct errors to verify both count and contents are preserved
        var ERROR_ONE = new QueryValidationError("Name", "must not be empty", "", "NotEmpty");
        var ERROR_TWO = new QueryValidationError("Age", "must be greater than zero", -1, "GreaterThan");
        var errors = new List<QueryValidationError> { ERROR_ONE, ERROR_TWO };

        // Act
        var exception = new QueryValidationException(errors);

        // Assert — the exception exposes exactly the errors passed in
        exception.Errors.Count.ShouldBe(2);
        exception.Errors.ShouldContain(ERROR_ONE);
        exception.Errors.ShouldContain(ERROR_TWO);

        // Assert — it is an Exception so the pipeline propagates it normally
        exception.ShouldBeAssignableTo<Exception>();

        // Assert — the message summarises the failure count
        exception.Message.ShouldContain("2");
    }
}
