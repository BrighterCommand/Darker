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
    public class ScopedServiceDisposeThrowsTests
    {
        private readonly LoggerCaptureFixture _logs;

        public ScopedServiceDisposeThrowsTests(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public void When_a_scoped_service_throws_on_dispose_inside_the_per_query_scope_should_return_the_result_and_log_at_error()
        {
            // Arrange — a Scoped service whose Dispose throws, resolved inside the per-query scope
            _logs.Clear();
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(_logs.Provider));
            services.AddScoped<ThrowingDisposeDependency>();
            services.AddTransient<ThrowingDisposeDependencyQueryHandler>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(r => r.Register<ThrowingDisposeDependencyQuery, ThrowingDisposeDependencyResult, ThrowingDisposeDependencyQueryHandler>());

            var queryProcessor = services.BuildServiceProvider().GetRequiredService<IQueryProcessor>();

            // Act — tearing down the scope disposes the dependency, which throws
            ThrowingDisposeDependencyResult result = null;
            Should.NotThrow(() => result = queryProcessor.Execute(new ThrowingDisposeDependencyQuery()));

            // Assert — the query returns its result
            result.Value.ShouldBe(ThrowingDisposeDependencyQueryHandler.RESULT);

            // Assert — the dependency's fault is logged once at Error
            _logs.CapturedLogs
                .Count(e => e.LogLevel == LogLevel.Error && e.Exception is DependencyDisposeFaultException)
                .ShouldBe(1);
        }
    }
}
