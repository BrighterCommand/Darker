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

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Caching.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

/// <summary>
/// End-to-end pipeline test: verifies FR13 — when the configured <see cref="HybridCache"/>
/// cannot serialize the handler's result type on a cache miss, the serialization exception
/// surfaces to the caller. The caching decorator must NOT swallow the exception or silently
/// return an uncached result without signal.
/// </summary>
public class SerializationFailureTests
{
    [Fact]
    public async Task When_result_cannot_be_serialized_should_surface_exception()
    {
        // Arrange — build a real ServiceProvider with the full Darker DI pipeline.
        // A ThrowingHybridCacheSerializer is registered for SerializationFailureResult so
        // that HybridCache throws when it attempts to serialize the factory result on a miss.
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddHybridCache()
            .AddSerializer<SerializationFailureResult, ThrowingHybridCacheSerializer>();
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(SerializationFailureQueryHandlerAsync).Assembly)
            .AddCaching();

        await using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

        var QUERY = new SerializationFailureQuery { Payload = "boom" };

        // Act & Assert — ExecuteAsync must throw the serializer's exception (not swallow it).
        // The decorator calls GetOrCreateAsync; on a miss the factory runs, HybridCache
        // serializes the result with ThrowingHybridCacheSerializer.Serialize which throws
        // NotSupportedException; that exception must propagate out of ExecuteAsync unmodified.
        await Should.ThrowAsync<NotSupportedException>(
            () => queryProcessor.ExecuteAsync(QUERY));
    }
}
