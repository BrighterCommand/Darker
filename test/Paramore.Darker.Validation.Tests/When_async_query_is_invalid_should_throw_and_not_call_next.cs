using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Validation.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.Tests;

public class ValidateQueryDecoratorAsyncInvalidQueryTests
{
    [Fact]
    public async Task When_async_query_is_invalid_should_throw_and_not_call_next()
    {
        // Arrange — two errors to verify the full collection is carried through to the exception
        var EXPECTED_ERROR_ONE = new QueryValidationError("Name", "must not be empty", "", "NotEmpty");
        var EXPECTED_ERROR_TWO = new QueryValidationError("Age", "must be greater than zero", -1, "GreaterThan");
        var errors = new List<QueryValidationError> { EXPECTED_ERROR_ONE, EXPECTED_ERROR_TWO };

        var query = new ValidationTestQuery();
        var decorator = new StubValidateQueryDecoratorAsync<ValidationTestQuery, ValidationTestQuery.Result>(errors);

        var nextCallCount = 0;
        Task<ValidationTestQuery.Result> Next(ValidationTestQuery q, CancellationToken ct)
        {
            nextCallCount++;
            return Task.FromResult(new ValidationTestQuery.Result());
        }
        Task<ValidationTestQuery.Result> Fallback(ValidationTestQuery q, CancellationToken ct)
            => Task.FromResult(new ValidationTestQuery.Result());

        // Act — invoke ExecuteAsync on a decorator configured to return validation errors
        var exception = await Should.ThrowAsync<QueryValidationException>(
            () => decorator.ExecuteAsync(query, Next, Fallback, CancellationToken.None));

        // Assert — the exception carries exactly the errors returned by ValidateAsync
        exception.Errors.Count.ShouldBe(2);
        exception.Errors.ShouldContain(EXPECTED_ERROR_ONE);
        exception.Errors.ShouldContain(EXPECTED_ERROR_TWO);

        // Assert — next was never invoked (pipeline short-circuits on validation failure)
        nextCallCount.ShouldBe(0);
    }
}
