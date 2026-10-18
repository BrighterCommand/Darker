using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Logging;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.Logging
{
    [Collection("DarkerHostBootstrap")]
    public class LoggingProviderThrowsDuringAsyncTeardownTests
    {
        [Fact]
        public async Task When_the_logging_provider_throws_while_async_teardown_logs_a_lifetime_fault_on_execute_async_should_still_return_the_result()
        {
            // Arrange — a Scoped service whose DisposeAsync faults, and a logging provider that throws at Error
            var queryProcessor = LifetimeFaultProcessor(r => r.AddAsyncHandlers(
                x => x.Register<LoggingFaultAsyncQuery<LifetimeFaultTag>, LoggingFaultAsyncResult<LifetimeFaultTag>, LoggingFaultAsyncLifetimeHandler>()),
                services => services.AddTransient<LoggingFaultAsyncLifetimeHandler>());

            // Act — teardown logs the dependency's fault, and the logging call itself throws
            LoggingFaultAsyncResult<LifetimeFaultTag> result = null;
            await Should.NotThrowAsync(async () => result = await queryProcessor.ExecuteAsync(new LoggingFaultAsyncQuery<LifetimeFaultTag>()));

            // Assert
            result.Value.ShouldBe(LoggingFaultAsyncLifetimeHandler.RESULT);
        }

        [Fact]
        public async Task When_the_logging_provider_throws_while_async_teardown_logs_a_lifetime_fault_on_execute_stream_should_still_yield_every_item()
        {
            // Arrange — a Scoped service whose DisposeAsync faults, and a logging provider that throws at Error
            var queryProcessor = LifetimeFaultProcessor(r => r.AddStreamHandlers(
                x => x.Register<LoggingFaultStreamQuery<LifetimeFaultTag>, LoggingFaultStreamItem<LifetimeFaultTag>, LoggingFaultStreamLifetimeHandler>()),
                services => services.AddTransient<LoggingFaultStreamLifetimeHandler>());
            var items = new List<LoggingFaultStreamItem<LifetimeFaultTag>>();

            // Act — the enumeration ends by tearing the scope down, logging the fault, and the logging call itself throws
            await Should.NotThrowAsync(async () =>
            {
                await foreach (var item in queryProcessor.ExecuteStream(new LoggingFaultStreamQuery<LifetimeFaultTag>()))
                    items.Add(item);
            });

            // Assert
            items.Count.ShouldBe(2);
        }

        [Fact]
        public async Task When_the_logging_provider_throws_while_async_teardown_logs_a_release_fault_on_execute_async_should_still_return_the_result()
        {
            // Arrange — a handler whose DisposeAsync faults, and a logger factory that throws at Error
            var previousFactory = ApplicationLogging.LoggerFactory;
            ApplicationLogging.LoggerFactory = new LoggerFactory(new ILoggerProvider[] { new ThrowingLoggerProvider() });
            try
            {
                var registryAsync = new QueryHandlerRegistryAsync();
                registryAsync.Register<LoggingFaultAsyncQuery<ReleaseFaultTag>, LoggingFaultAsyncResult<ReleaseFaultTag>, LoggingFaultAsyncReleaseHandler>();
                var queryProcessor = SimpleFactoryProcessor(
                    _ => new LoggingFaultAsyncReleaseHandler(), registryAsync, new StreamQueryHandlerRegistry());

                // Act — releasing the handler faults, teardown logs it, and the logging call itself throws
                LoggingFaultAsyncResult<ReleaseFaultTag> result = null;
                await Should.NotThrowAsync(async () => result = await queryProcessor.ExecuteAsync(new LoggingFaultAsyncQuery<ReleaseFaultTag>()));

                // Assert
                result.Value.ShouldBe(LoggingFaultAsyncReleaseHandler.RESULT);
            }
            finally
            {
                ApplicationLogging.LoggerFactory = previousFactory;
            }
        }

        [Fact]
        public async Task When_the_logging_provider_throws_while_async_teardown_logs_a_release_fault_on_execute_stream_should_still_yield_every_item()
        {
            // Arrange — a handler whose DisposeAsync faults, and a logger factory that throws at Error
            var previousFactory = ApplicationLogging.LoggerFactory;
            ApplicationLogging.LoggerFactory = new LoggerFactory(new ILoggerProvider[] { new ThrowingLoggerProvider() });
            try
            {
                var registryStream = new StreamQueryHandlerRegistry();
                registryStream.Register<LoggingFaultStreamQuery<ReleaseFaultTag>, LoggingFaultStreamItem<ReleaseFaultTag>, LoggingFaultStreamReleaseHandler>();
                var queryProcessor = SimpleFactoryProcessor(
                    _ => new LoggingFaultStreamReleaseHandler(), new QueryHandlerRegistryAsync(), registryStream);
                var items = new List<LoggingFaultStreamItem<ReleaseFaultTag>>();

                // Act — releasing the handler faults, teardown logs it, and the logging call itself throws
                await Should.NotThrowAsync(async () =>
                {
                    await foreach (var item in queryProcessor.ExecuteStream(new LoggingFaultStreamQuery<ReleaseFaultTag>()))
                        items.Add(item);
                });

                // Assert
                items.Count.ShouldBe(2);
            }
            finally
            {
                ApplicationLogging.LoggerFactory = previousFactory;
            }
        }

        private static IQueryProcessor LifetimeFaultProcessor(
            System.Func<IDarkerHandlerBuilder, IDarkerHandlerBuilder> register, System.Action<IServiceCollection> addHandler)
        {
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddProvider(new ThrowingLoggerProvider()));
            services.AddScoped<FaultingAsyncDisposeDependency>();
            addHandler(services);
            register(services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped));
            return services.BuildServiceProvider().GetRequiredService<IQueryProcessor>();
        }

        private static QueryProcessor SimpleFactoryProcessor(
            System.Func<System.Type, IQueryHandler> createHandler, QueryHandlerRegistryAsync registryAsync, StreamQueryHandlerRegistry registryStream)
        {
            var handlerFactory = new SimpleHandlerFactory(createHandler);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                registryAsync, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                registryStream);
            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }
    }
}
