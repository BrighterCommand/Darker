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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker;
using Paramore.Darker.Validation;
using Paramore.Darker.Validation.FluentValidation.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.FluentValidation.Tests;

public class FluentValidationQueryValidatorDecoratorAsyncInvalidQueryTests
{
    [Fact]
    public async Task When_async_fluent_validator_fails_should_map_failures_and_throw()
    {
        // Arrange — a query that violates two distinct rules: Name is empty, Age is negative
        var INVALID_NAME = string.Empty;
        const int INVALID_AGE = -1;
        var query = new FvTestQuery { Name = INVALID_NAME, Age = INVALID_AGE };

        var serviceProvider = new ServiceCollection()
            .AddScoped<IValidator<FvTestQuery>, FvTestQueryValidator>()
            .BuildServiceProvider();

        // The decorator is closed over IQuery<TResult> at pipeline runtime (see PipelineBuilder:253)
        var decorator = new FluentValidationQueryValidatorDecoratorAsync<IQuery<FvTestQuery.Result>, FvTestQuery.Result>(serviceProvider);

        var nextCallCount = 0;

        Task<FvTestQuery.Result> Next(IQuery<FvTestQuery.Result> q, CancellationToken ct)
        {
            nextCallCount++;
            return Task.FromResult(new FvTestQuery.Result());
        }

        Task<FvTestQuery.Result> Fallback(IQuery<FvTestQuery.Result> q, CancellationToken ct)
            => Task.FromResult(new FvTestQuery.Result());

        // Act
        var exception = await Should.ThrowAsync<QueryValidationException>(
            () => decorator.ExecuteAsync(query, Next, Fallback, CancellationToken.None));

        // Assert — two failures are surfaced (FR6: the whole collection, not just the first)
        exception.Errors.Count.ShouldBe(2);

        var errors = exception.Errors.ToList();
        var propertyNames = errors.Select(e => e.PropertyName).ToList();
        propertyNames.ShouldContain("Name");
        propertyNames.ShouldContain("Age");

        // Assert — all four fields are mapped for at least the Name failure
        var nameError = errors.Single(e => e.PropertyName == "Name");
        nameError.PropertyName.ShouldBe("Name");
        nameError.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        nameError.AttemptedValue.ShouldBe(INVALID_NAME);
        nameError.ErrorCode.ShouldNotBeNullOrWhiteSpace();

        // Assert — next was never invoked (pipeline short-circuits on failure)
        nextCallCount.ShouldBe(0);
    }
}
