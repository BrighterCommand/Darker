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
/// End-to-end pipeline test: proves that executing a <c>[CacheableQueryAsync]</c>-annotated
/// query when no <see cref="Microsoft.Extensions.Caching.Hybrid.HybridCache"/> is registered
/// in DI throws <see cref="ConfigurationException"/> — failing fast rather than silently
/// bypassing the cache (FR12).
/// </summary>
public class MissingHybridCacheRegistrationTests
{
    [Fact]
    public async Task When_hybrid_cache_not_registered_should_throw_configuration_exception()
    {
        // Arrange — build a real QueryProcessor with AddCaching() but deliberately WITHOUT AddHybridCache().
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();
        // NOTE: services.AddHybridCache() is intentionally omitted to trigger FR12 fail-fast.
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(CacheTestQueryHandlerAsync).Assembly)
            .AddCaching();

        await using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

        var query = new CacheTestQuery { Payload = "test" };

        // Act & Assert — executing a cacheable query without HybridCache registered throws ConfigurationException.
        var exception = await Should.ThrowAsync<ConfigurationException>(
            async () => await queryProcessor.ExecuteAsync(query));

        // Assert — the exception message names the missing HybridCache registration.
        exception.Message.ShouldContain("HybridCache");
    }
}
