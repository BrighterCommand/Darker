#region Licence
/* The MIT License (MIT)
Copyright © 2026 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */
#endregion

using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker;
using Paramore.Darker.Validation.DataAnnotations;
using Paramore.Darker.Validation.DataAnnotations.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.DataAnnotations.Tests;

public class DataAnnotationsQueryValidatorDecoratorAsyncValidQueryTests
{
    [Fact]
    public async Task When_async_data_annotations_pass_should_call_next()
    {
        // Arrange — a query whose [Required]/[Range] constraints are all satisfied
        var EXPECTED_RESULT = new DaTestQuery.Result { Value = "success" };
        var query = new DaTestQuery { Name = "Valid Name", Age = 25 };
        using var cts = new CancellationTokenSource();
        var EXPECTED_TOKEN = cts.Token;

        // The decorator is closed over IQuery<TResult> at pipeline runtime (see PipelineBuilder:253)
        var decorator = new DataAnnotationsQueryValidatorDecoratorAsync<IQuery<DaTestQuery.Result>, DaTestQuery.Result>();

        var nextCallCount = 0;
        CancellationToken observedNextToken = CancellationToken.None;

        Task<DaTestQuery.Result> Next(IQuery<DaTestQuery.Result> q, CancellationToken ct)
        {
            nextCallCount++;
            observedNextToken = ct;
            return Task.FromResult(EXPECTED_RESULT);
        }

        Task<DaTestQuery.Result> Fallback(IQuery<DaTestQuery.Result> q, CancellationToken ct)
            => Task.FromResult(new DaTestQuery.Result());

        // Act
        var result = await decorator.ExecuteAsync(query, Next, Fallback, EXPECTED_TOKEN);

        // Assert — the result is the exact object returned by next, unchanged
        result.ShouldBeSameAs(EXPECTED_RESULT);

        // Assert — next was invoked exactly once
        nextCallCount.ShouldBe(1);

        // Assert — the CancellationToken is threaded to next
        observedNextToken.ShouldBe(EXPECTED_TOKEN);
    }
}
