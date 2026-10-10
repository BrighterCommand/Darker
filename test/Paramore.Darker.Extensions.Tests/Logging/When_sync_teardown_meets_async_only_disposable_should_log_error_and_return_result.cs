using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.Logging
{
    [Collection("DarkerHostBootstrap")]
    public class When_sync_teardown_meets_async_only_disposable_should_log_error_and_return_result
    {
        private readonly LoggerCaptureFixture _logs;

        public When_sync_teardown_meets_async_only_disposable_should_log_error_and_return_result(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public void When_execute_tears_down_a_scope_holding_an_IAsyncDisposable_only_service_should_log_error_and_return_result()
        {
            // Arrange — a Scoped service that can only be disposed asynchronously
            _logs.Clear();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(_logs.Provider));
            services.AddScoped<AsyncOnlyDisposableDependency>();
            services.AddTransient<AsyncOnlyDisposableQueryHandler>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(r => r.Register<AsyncOnlyDisposableQuery, AsyncOnlyDisposableResult, AsyncOnlyDisposableQueryHandler>());

            var queryProcessor = services.BuildServiceProvider().GetRequiredService<IQueryProcessor>();

            // Act — synchronous Execute tears the scope down with Dispose, which the container rejects
            AsyncOnlyDisposableResult result = null;
            Should.NotThrow(() => result = queryProcessor.Execute(new AsyncOnlyDisposableQuery()));

            // Assert — the query returns its result
            result.Value.ShouldBe(AsyncOnlyDisposableQueryHandler.RESULT);

            // Assert — the container's fault is logged once at Error, naming the offending service
            var error = _logs.CapturedLogs.Single(e => e.LogLevel == LogLevel.Error);
            error.Exception.ShouldBeOfType<InvalidOperationException>();
            error.Exception.Message.ShouldContain(nameof(AsyncOnlyDisposableDependency));
        }
    }
}
