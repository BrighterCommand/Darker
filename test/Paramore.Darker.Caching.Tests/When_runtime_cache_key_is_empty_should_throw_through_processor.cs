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
/// End-to-end pipeline test proving that executing an <see cref="IAmCacheable"/> query whose
/// <c>CacheKey</c> returns an empty string at runtime surfaces a <see cref="ConfigurationException"/>
/// through the real <see cref="QueryProcessor"/> pipeline, and that the handler never runs (FR4).
/// </summary>
/// <remarks>
/// Because <c>TQuery</c> is <c>IQuery&lt;TResult&gt;</c> at runtime in the Darker pipeline (not the
/// concrete query type), this test drives the full DI stack to prove the runtime
/// <c>query is IAmCacheable</c> detection in <see cref="DefaultCacheKeyGenerator"/> fails fast on
/// an empty key when flowing through the real processor — a directly-instantiated decorator would
/// exercise a different resolution path.
/// </remarks>
public class EmptyRuntimeCacheKeyPipelineTests
{
    [Fact]
    public async Task When_runtime_cache_key_is_empty_should_throw_through_processor()
    {
        // Arrange — real ServiceProvider with AddDarker + AddCaching + HybridCache.
        // HandlerCallCounter is a singleton so we can assert after the exception that
        // the handler body was never entered.
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();
        services.AddHybridCache();
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(EmptyCacheKeyPipelineQueryHandlerAsync).Assembly)
            .AddCaching();

        await using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
        var counter = provider.GetRequiredService<HandlerCallCounter>();

        // The query's IAmCacheable.CacheKey returns "" at runtime — the fail-fast condition (FR4).
        var query = new EmptyCacheKeyPipelineQuery();

        // Act — executing through the processor must surface ConfigurationException.
        await Should.ThrowAsync<ConfigurationException>(
            async () => await queryProcessor.ExecuteAsync(query));

        // Assert — the handler never ran; the exception occurred during key generation, before next.
        counter.CallCount.ShouldBe(0,
            "The handler must not execute when IAmCacheable.CacheKey returns an empty string at runtime");
    }
}
