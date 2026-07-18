using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Validation.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.Tests;

public class ValidateQueryDecoratorAsyncValidQueryTests
{
    [Fact]
    public async Task When_async_query_is_valid_should_call_next()
    {
        // Arrange — an empty error list signals that the query is valid; track invocations via counter
        var EXPECTED_RESULT = new ValidationTestQuery.Result { Value = "hello" };
        var query = new ValidationTestQuery();
        var decorator = new StubValidateQueryDecoratorAsync<ValidationTestQuery, ValidationTestQuery.Result>(
            new List<QueryValidationError>());

        var nextCallCount = 0;
        Task<ValidationTestQuery.Result> Next(ValidationTestQuery q, CancellationToken ct)
        {
            nextCallCount++;
            return Task.FromResult(EXPECTED_RESULT);
        }
        Task<ValidationTestQuery.Result> Fallback(ValidationTestQuery q, CancellationToken ct)
            => Task.FromResult(new ValidationTestQuery.Result());

        // Act
        var result = await decorator.ExecuteAsync(query, Next, Fallback, CancellationToken.None);

        // Assert — the result is the exact object returned by next, unchanged
        result.ShouldBeSameAs(EXPECTED_RESULT);

        // Assert — next was invoked exactly once
        nextCallCount.ShouldBe(1);
    }
}
