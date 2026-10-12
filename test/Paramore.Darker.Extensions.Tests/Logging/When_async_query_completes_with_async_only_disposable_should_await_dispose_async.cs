using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.Logging
{
    [Collection("DarkerHostBootstrap")]
    public class When_async_query_completes_with_async_only_disposable_should_await_dispose_async
    {
        private readonly LoggerCaptureFixture _logs;

        public When_async_query_completes_with_async_only_disposable_should_await_dispose_async(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public async Task When_execute_async_completes_with_an_IAsyncDisposable_only_service_should_await_its_dispose_async()
        {
            // Arrange — a Scoped service that can only be disposed asynchronously, and only after a yield
            _logs.Clear();
            var probe = new AsyncDisposeProbe();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(_logs.Provider));
            services.AddSingleton(probe);
            services.AddScoped<YieldingAsyncOnlyDisposableDependency>();
            services.AddTransient<AsyncTeardownQueryHandler>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddAsyncHandlers(r => r.Register<AsyncTeardownQuery, AsyncTeardownResult, AsyncTeardownQueryHandler>());

            var queryProcessor = services.BuildServiceProvider().GetRequiredService<IQueryProcessor>();

            // Act
            var result = await queryProcessor.ExecuteAsync(new AsyncTeardownQuery());

            // Assert — the result is returned, and the async disposal had finished by the time we got it back
            result.Value.ShouldBe(AsyncTeardownQueryHandler.RESULT);
            probe.DisposedAsync.ShouldBeTrue();

            // Assert — nothing was logged at Error
            _logs.CapturedLogs.Where(e => e.LogLevel == LogLevel.Error).ShouldBeEmpty();
        }

        [Fact]
        public async Task When_execute_stream_completes_with_an_IAsyncDisposable_only_service_should_await_its_dispose_async()
        {
            // Arrange — a Scoped service that can only be disposed asynchronously, and only after a yield
            _logs.Clear();
            var probe = new AsyncDisposeProbe();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(_logs.Provider));
            services.AddSingleton(probe);
            services.AddScoped<YieldingAsyncOnlyDisposableDependency>();
            services.AddTransient<AsyncTeardownStreamQueryHandler>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddStreamHandlers(r => r.Register<AsyncTeardownStreamQuery, AsyncTeardownStreamItem, AsyncTeardownStreamQueryHandler>());

            var queryProcessor = services.BuildServiceProvider().GetRequiredService<IQueryProcessor>();
            var items = new List<AsyncTeardownStreamItem>();

            // Act
            await foreach (var item in queryProcessor.ExecuteStream(new AsyncTeardownStreamQuery()))
                items.Add(item);

            // Assert — the items are yielded, and the async disposal had finished when the enumeration ended
            items.Single().Value.ShouldBe(AsyncTeardownStreamQueryHandler.ITEM);
            probe.DisposedAsync.ShouldBeTrue();

            // Assert — nothing was logged at Error
            _logs.CapturedLogs.Where(e => e.LogLevel == LogLevel.Error).ShouldBeEmpty();
        }
    }
}
