using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Builder;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Logging;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.Logging
{
    [Collection("DarkerHostBootstrap")]
    public class When_logging_provider_throws_during_teardown_should_still_return_result
    {
        [Fact]
        public void When_the_logging_provider_throws_while_teardown_logs_a_lifetime_fault_should_still_return_the_result()
        {
            // Arrange — a Scoped service whose Dispose throws, and a logging provider that throws at Error
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(new ThrowingLoggerProvider()));
            services.AddScoped<ThrowingDisposeDependency>();
            services.AddTransient<LoggingFaultLifetimeQueryHandler>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(r => r.Register<LoggingFaultLifetimeQuery, LoggingFaultLifetimeResult, LoggingFaultLifetimeQueryHandler>());

            var queryProcessor = services.BuildServiceProvider().GetRequiredService<IQueryProcessor>();

            // Act — teardown logs the dependency's fault, and the logging call itself throws
            LoggingFaultLifetimeResult result = null;
            Should.NotThrow(() => result = queryProcessor.Execute(new LoggingFaultLifetimeQuery()));

            // Assert
            result.Value.ShouldBe(LoggingFaultLifetimeQueryHandler.RESULT);
        }

        [Fact]
        public void When_the_logging_provider_throws_while_teardown_logs_a_release_fault_should_still_return_the_result()
        {
            // Arrange — a handler whose Dispose throws, and a logger factory that throws at Error
            var previousFactory = ApplicationLogging.LoggerFactory;
            ApplicationLogging.LoggerFactory = new LoggerFactory(new ILoggerProvider[] { new ThrowingLoggerProvider() });
            try
            {
                var registry = new QueryHandlerRegistry();
                registry.Register<LoggingFaultReleaseQuery, LoggingFaultReleaseResult, LoggingFaultReleaseQueryHandler>();

                var queryProcessor = QueryProcessorBuilder.With()
                    .Handlers(registry, _ => new LoggingFaultReleaseQueryHandler(), _ => { }, _ => null)
                    .InMemoryQueryContextFactory()
                    .Build();

                // Act — releasing the handler throws, teardown logs it, and the logging call itself throws
                LoggingFaultReleaseResult result = null;
                Should.NotThrow(() => result = queryProcessor.Execute(new LoggingFaultReleaseQuery()));

                // Assert
                result.Value.ShouldBe(LoggingFaultReleaseQueryHandler.RESULT);
            }
            finally
            {
                ApplicationLogging.LoggerFactory = previousFactory;
            }
        }
    }
}
