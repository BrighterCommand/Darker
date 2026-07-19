using Paramore.Darker.Validation;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.Tests;

public class QueryValidationErrorTests
{
    [Fact]
    public void When_creating_query_validation_error_should_expose_supplied_values()
    {
        // Arrange
        const string PROPERTY_NAME = "Name";
        const string ERROR_MESSAGE = "must not be empty";
        const string ATTEMPTED_VALUE = "some value";
        const string ERROR_CODE = "NotEmpty";

        // Act
        var errorWithDefaults = new QueryValidationError(PROPERTY_NAME, ERROR_MESSAGE);
        var errorWithAll = new QueryValidationError(PROPERTY_NAME, ERROR_MESSAGE, ATTEMPTED_VALUE, ERROR_CODE);
        var errorWithAllDuplicate = new QueryValidationError(PROPERTY_NAME, ERROR_MESSAGE, ATTEMPTED_VALUE, ERROR_CODE);

        // Assert — minimal constructor exposes PropertyName/ErrorMessage; optional fields default to null
        errorWithDefaults.PropertyName.ShouldBe(PROPERTY_NAME);
        errorWithDefaults.ErrorMessage.ShouldBe(ERROR_MESSAGE);
        errorWithDefaults.AttemptedValue.ShouldBeNull();
        errorWithDefaults.ErrorCode.ShouldBeNull();

        // Assert — value equality: two records with identical arguments are equal
        errorWithAll.ShouldBe(errorWithAllDuplicate);
    }
}
