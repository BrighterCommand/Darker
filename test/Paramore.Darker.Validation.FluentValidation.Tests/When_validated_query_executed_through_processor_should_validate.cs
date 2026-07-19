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

using System.Threading.Tasks;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Validation;
using Paramore.Darker.Validation.FluentValidation.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Validation.FluentValidation.Tests;

/// <summary>
/// End-to-end pipeline test: proves that validation runs through the real
/// <see cref="QueryProcessor"/> — including runtime-type validator resolution via
/// <c>IServiceProvider</c> — not just through an isolated decorator instance.
/// </summary>
/// <remarks>
/// Isolation tests that instantiate the decorator directly or resolve it closed over the
/// concrete query type exercise a resolution path that the pipeline never uses
/// (<c>PipelineBuilder</c> closes all decorators over <c>IQuery&lt;TResult&gt;</c>).
/// This test exercises the actual end-to-end path: attribute discovery → abstract-type
/// resolution via <c>UseFluentValidation()</c> → runtime-type validator lookup →
/// pass-through or exception (review Finding 2).
/// </remarks>
public class ValidatedQueryProcessorPipelineTests
{
    [Fact]
    public async Task When_validated_query_executed_through_processor_should_validate()
    {
        // Arrange — build a real ServiceProvider with the full Darker DI pipeline
        FvTestQueryHandlerAsync.Reset();

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(FvTestQueryHandlerAsync).Assembly)
            .UseFluentValidation();
        services.AddScoped<IValidator<FvTestQuery>, FvTestQueryValidator>();

        await using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

        // Act — a valid query must reach the handler and return its result
        var VALID_QUERY = new FvTestQuery { Name = "Alice", Age = 30 };
        var result = await queryProcessor.ExecuteAsync(VALID_QUERY);

        // Assert — handler ran and returned the expected result
        result.ShouldNotBeNull();
        result.Value.ShouldBe("Alice");
        FvTestQueryHandlerAsync.HandlerExecuted.ShouldBeTrue();

        // Arrange — reset handler-ran flag before the invalid scenario
        FvTestQueryHandlerAsync.Reset();

        // Act & Assert — an invalid query must throw QueryValidationException
        var INVALID_QUERY = new FvTestQuery { Name = string.Empty, Age = 0 };
        await Should.ThrowAsync<QueryValidationException>(
            () => queryProcessor.ExecuteAsync(INVALID_QUERY));

        // Assert — the handler body was never entered for the invalid query
        FvTestQueryHandlerAsync.HandlerExecuted.ShouldBeFalse();
    }
}
