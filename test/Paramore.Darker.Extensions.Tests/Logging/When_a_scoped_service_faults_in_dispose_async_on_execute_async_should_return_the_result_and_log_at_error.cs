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
    public class ScopedServiceDisposeAsyncFaultsTests
    {
        private readonly LoggerCaptureFixture _logs;

        public ScopedServiceDisposeAsyncFaultsTests(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public async Task When_a_scoped_service_faults_in_dispose_async_on_execute_async_should_return_the_result_and_log_at_error()
        {
            // Arrange — a Scoped service whose DisposeAsync faults, resolved inside the per-query scope
            _logs.Clear();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(_logs.Provider));
            services.AddScoped<FaultingAsyncDisposeDependency>();
            services.AddTransient<FaultingAsyncDisposeQueryHandler>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddAsyncHandlers(r => r.Register<FaultingAsyncDisposeQuery, FaultingAsyncDisposeResult, FaultingAsyncDisposeQueryHandler>());

            var queryProcessor = services.BuildServiceProvider().GetRequiredService<IQueryProcessor>();

            // Act — tearing down the scope disposes the dependency, which faults
            FaultingAsyncDisposeResult result = null;
            await Should.NotThrowAsync(async () => result = await queryProcessor.ExecuteAsync(new FaultingAsyncDisposeQuery()));

            // Assert — the query returns its result
            result.Value.ShouldBe(FaultingAsyncDisposeQueryHandler.RESULT);

            // Assert — the dependency's fault is logged once at Error
            _logs.CapturedLogs
                .Count(e => e.LogLevel == LogLevel.Error && e.Exception is DependencyDisposeAsyncFaultException)
                .ShouldBe(1);
        }

        [Fact]
        public async Task When_a_scoped_service_faults_in_dispose_async_on_execute_stream_should_yield_every_item_and_log_at_error()
        {
            // Arrange — a Scoped service whose DisposeAsync faults, resolved inside the per-query scope
            _logs.Clear();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(_logs.Provider));
            services.AddScoped<FaultingAsyncDisposeDependency>();
            services.AddTransient<FaultingAsyncDisposeStreamQueryHandler>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddStreamHandlers(r => r.Register<FaultingAsyncDisposeStreamQuery, FaultingAsyncDisposeStreamItem, FaultingAsyncDisposeStreamQueryHandler>());

            var queryProcessor = services.BuildServiceProvider().GetRequiredService<IQueryProcessor>();
            var items = new List<FaultingAsyncDisposeStreamItem>();

            // Act — the enumeration ends by tearing down the scope, which faults
            await Should.NotThrowAsync(async () =>
            {
                await foreach (var item in queryProcessor.ExecuteStream(new FaultingAsyncDisposeStreamQuery()))
                    items.Add(item);
            });

            // Assert — every item was yielded
            items.Select(i => i.Value).ShouldBe(new[]
            {
                FaultingAsyncDisposeStreamQueryHandler.ITEM, FaultingAsyncDisposeStreamQueryHandler.ITEM
            });

            // Assert — the dependency's fault is logged once at Error
            _logs.CapturedLogs
                .Count(e => e.LogLevel == LogLevel.Error && e.Exception is DependencyDisposeAsyncFaultException)
                .ShouldBe(1);
        }
    }
}
