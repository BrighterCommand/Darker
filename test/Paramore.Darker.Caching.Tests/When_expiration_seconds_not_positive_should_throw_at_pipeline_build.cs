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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Caching.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

/// <summary>
/// End-to-end pipeline tests: prove that a <c>[CacheableQueryAsync]</c> attribute with a
/// non-positive <c>expirationSeconds</c> throws <see cref="ConfigurationException"/> at
/// pipeline BUILD time — i.e. before the handler body is ever entered — when
/// <see cref="CacheableQueryDecoratorAsync{TQuery,TResult}.InitializeFromAttributeParams"/>
/// validates the supplied value. Both zero and negative values are covered in the single
/// test method so the RALPH-VERIFY filter matches the method name.
/// </summary>
public class NonPositiveExpirationSecondsBuildTests
{
    [Fact]
    public async Task When_expiration_seconds_not_positive_should_throw_at_pipeline_build()
    {
        // Arrange — a real QueryProcessor with handlers for both zero and negative expiry values.
        // Both ZeroExpiryQueryHandlerAsync and NegativeExpiryQueryHandlerAsync live in this assembly
        // and are discovered by AddHandlersFromAssemblies.
        // HandlerCallCounter is a singleton shared across all handlers; it must stay at zero
        // for both queries to prove the failure happened at pipeline build, not inside the handler.
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();
        services.AddHybridCache();
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(ZeroExpiryQueryHandlerAsync).Assembly)
            .AddCaching();

        await using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
        var counter = provider.GetRequiredService<HandlerCallCounter>();

        // Act & Assert — expirationSeconds: 0 throws at pipeline build
        await Should.ThrowAsync<ConfigurationException>(
            async () => await queryProcessor.ExecuteAsync(new ZeroExpiryQuery()));

        // Assert — handler body was never entered for zero expiry
        counter.CallCount.ShouldBe(0);

        // Act & Assert — expirationSeconds: -5 likewise throws at pipeline build
        await Should.ThrowAsync<ConfigurationException>(
            async () => await queryProcessor.ExecuteAsync(new NegativeExpiryQuery()));

        // Assert — handler body was never entered for negative expiry either
        counter.CallCount.ShouldBe(0);
    }
}
