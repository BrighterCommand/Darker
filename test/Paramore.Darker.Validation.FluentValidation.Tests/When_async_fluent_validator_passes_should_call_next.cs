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
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker;
using Paramore.Darker.Validation.FluentValidation.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.FluentValidation.Tests;

public class FluentValidationQueryValidatorDecoratorAsyncValidQueryTests
{
    [Fact]
    public async Task When_async_fluent_validator_passes_should_call_next()
    {
        // Arrange — build a real ServiceProvider with a passing async validator for FvTestQuery
        var EXPECTED_RESULT = new FvTestQuery.Result { Value = "success" };
        var query = new FvTestQuery { Name = "Valid Name" };
        using var cts = new CancellationTokenSource();
        var EXPECTED_TOKEN = cts.Token;

        var capturingValidator = new CapturingAsyncValidator();
        var serviceProvider = new ServiceCollection()
            .AddScoped<IValidator<FvTestQuery>>(_ => capturingValidator)
            .BuildServiceProvider();

        // The decorator is closed over IQuery<TResult> at pipeline runtime (see PipelineBuilder:253)
        var decorator = new FluentValidationQueryValidatorDecoratorAsync<IQuery<FvTestQuery.Result>, FvTestQuery.Result>(serviceProvider);

        var nextCallCount = 0;
        CancellationToken observedNextToken = CancellationToken.None;

        Task<FvTestQuery.Result> Next(IQuery<FvTestQuery.Result> q, CancellationToken ct)
        {
            nextCallCount++;
            observedNextToken = ct;
            return Task.FromResult(EXPECTED_RESULT);
        }

        Task<FvTestQuery.Result> Fallback(IQuery<FvTestQuery.Result> q, CancellationToken ct)
            => Task.FromResult(new FvTestQuery.Result());

        // Act
        var result = await decorator.ExecuteAsync(query, Next, Fallback, EXPECTED_TOKEN);

        // Assert — the result is the exact object returned by next, unchanged
        result.ShouldBeSameAs(EXPECTED_RESULT);

        // Assert — next was invoked exactly once
        nextCallCount.ShouldBe(1);

        // Assert — the CancellationToken is threaded to next
        observedNextToken.ShouldBe(EXPECTED_TOKEN);

        // Assert — the CancellationToken is threaded to FluentValidation ValidateAsync
        capturingValidator.CapturedToken.ShouldBe(EXPECTED_TOKEN);
    }

    /// <summary>
    /// An async FluentValidation validator for <see cref="FvTestQuery"/> that records the
    /// <see cref="CancellationToken"/> supplied by the caller so tests can assert it was
    /// correctly threaded through.
    /// </summary>
    private sealed class CapturingAsyncValidator : AbstractValidator<FvTestQuery>
    {
        public CancellationToken CapturedToken { get; private set; }

        public CapturingAsyncValidator()
        {
            RuleFor(x => x.Name)
                .MustAsync((name, ct) =>
                {
                    CapturedToken = ct;
                    return Task.FromResult(true);
                });
        }
    }
}
