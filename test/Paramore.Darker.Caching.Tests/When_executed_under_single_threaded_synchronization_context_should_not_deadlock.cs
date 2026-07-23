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
using System.Threading;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Caching.Tests.TestDoubles;
using Paramore.Darker.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Caching.Tests;

/// <summary>
/// Proves the sync caching decorator does not deadlock when <c>Execute</c> runs on a thread
/// carrying a single-threaded <see cref="SynchronizationContext"/> (classic ASP.NET / UI thread)
/// and the cache resolves asynchronously.
/// </summary>
/// <remarks>
/// Uses <see cref="AsyncOnMissHybridCache"/>, whose <c>GetOrCreateAsync</c> awaits
/// <c>Task.Yield()</c> without <c>ConfigureAwait(false)</c>, so it captures the ambient
/// <see cref="NonPumpingSynchronizationContext"/>. If the decorator blocks the originating thread
/// while that continuation is queued to the never-pumped context, the call deadlocks — which this
/// test detects via a join timeout rather than hanging the suite.
/// </remarks>
public class SyncCacheSynchronizationContextTests
{
    [Fact]
    public void When_executed_under_single_threaded_synchronization_context_should_not_deadlock()
    {
        // Arrange — AsyncOnMissHybridCache forces the async slow path that captures the ambient
        // SynchronizationContext.
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(LoggerFactory.Create(_ => { }));
        services.AddSingleton<HandlerCallCounter>();
        services.AddSingleton<HybridCache, AsyncOnMissHybridCache>();
        services
            .AddDarker()
            .AddHandlersFromAssemblies(typeof(SyncCacheTestQueryHandler).Assembly)
            .AddCaching();

        using var provider = services.BuildServiceProvider();
        var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

        var QUERY = new SyncCacheTestQuery { Payload = "sync-context-payload" };

        string resultValue = null;
        Exception error = null;

        // Run Execute on a dedicated thread that carries a non-pumping single-threaded context.
        var worker = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new NonPumpingSynchronizationContext());
            try
            {
                resultValue = queryProcessor.Execute(QUERY).Value;
            }
            catch (Exception ex)
            {
                error = ex;
            }
        })
        {
            IsBackground = true
        };

        // Act — start the worker and wait with a timeout. A deadlock never completes, so the join
        // returns false rather than the suite hanging forever.
        worker.Start();
        var completed = worker.Join(TimeSpan.FromSeconds(10));

        // Assert — Execute completed (no deadlock) and returned the correct result.
        completed.ShouldBeTrue("Execute deadlocked under a single-threaded SynchronizationContext");
        error.ShouldBeNull();
        resultValue.ShouldBe("sync-context-payload");
    }
}
