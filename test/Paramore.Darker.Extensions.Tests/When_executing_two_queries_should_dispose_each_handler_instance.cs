using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class When_handler_lifetime_is_scoped_or_transient_should_dispose_disposable_handler_each_query
    {
        [Theory]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Transient)]
        public void When_executing_two_queries_should_dispose_each_handler_instance(ServiceLifetime handlerLifetime)
        {
            // Arrange
            var provider = Build(handlerLifetime);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var log = provider.GetRequiredService<DisposableHandlerLog>();

            // Act
            queryProcessor.Execute(new DisposableHandlerQuery());
            queryProcessor.Execute(new DisposableHandlerQuery());

            // Assert — two distinct handler instances, both disposed by the end of their query
            log.Handlers.Count.ShouldBe(2);
            log.Handlers[0].ShouldNotBeSameAs(log.Handlers[1]);
            foreach (var handler in log.Handlers)
                handler.IsDisposed.ShouldBeTrue();
        }

        [Theory]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Transient)]
        public async Task When_executing_two_async_queries_should_dispose_each_handler_instance(ServiceLifetime handlerLifetime)
        {
            // Arrange
            var provider = Build(handlerLifetime);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var log = provider.GetRequiredService<DisposableHandlerLog>();

            // Act
            await queryProcessor.ExecuteAsync(new DisposableHandlerQuery(), cancellationToken: TestContext.Current.CancellationToken);
            await queryProcessor.ExecuteAsync(new DisposableHandlerQuery(), cancellationToken: TestContext.Current.CancellationToken);

            // Assert — two distinct handler instances, both disposed by the end of their query
            log.Handlers.Count.ShouldBe(2);
            log.Handlers[0].ShouldNotBeSameAs(log.Handlers[1]);
            foreach (var handler in log.Handlers)
                handler.IsDisposed.ShouldBeTrue();
        }

        [Theory]
        [InlineData(ServiceLifetime.Scoped)]
        [InlineData(ServiceLifetime.Transient)]
        public async Task When_executing_two_stream_queries_should_dispose_each_handler_instance(ServiceLifetime handlerLifetime)
        {
            // Arrange
            var provider = Build(handlerLifetime);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var log = provider.GetRequiredService<DisposableHandlerLog>();

            // Act — enumerate each stream to completion
            await foreach (var _ in queryProcessor.ExecuteStream(new DisposableHandlerStreamQuery(), cancellationToken: TestContext.Current.CancellationToken)) { }
            await foreach (var _ in queryProcessor.ExecuteStream(new DisposableHandlerStreamQuery(), cancellationToken: TestContext.Current.CancellationToken)) { }

            // Assert — two distinct handler instances, both disposed by the end of their query
            log.Handlers.Count.ShouldBe(2);
            log.Handlers[0].ShouldNotBeSameAs(log.Handlers[1]);
            foreach (var handler in log.Handlers)
                handler.IsDisposed.ShouldBeTrue();
        }

        private static ServiceProvider Build(ServiceLifetime handlerLifetime)
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton<DisposableHandlerLog>();
            services.AddDarker(options => options.HandlerLifetime = handlerLifetime)
                .AddHandlers(registry => registry.Register<DisposableHandlerQuery, string, DisposableTrackedHandler>())
                .AddAsyncHandlers(registry => registry.Register<DisposableHandlerQuery, string, DisposableTrackedHandlerAsync>())
                .AddStreamHandlers(registry => registry.Register<DisposableHandlerStreamQuery, string, DisposableTrackedStreamHandler>());

            return services.BuildServiceProvider();
        }
    }
}
