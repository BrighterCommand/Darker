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
    public class When_handler_lifetime_is_singleton_should_resolve_handler_and_never_dispose_it
    {
        [Fact]
        public void When_executing_two_queries_should_use_one_handler_and_not_dispose_it()
        {
            // Arrange
            var provider = Build();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var log = provider.GetRequiredService<DisposableHandlerLog>();

            // Act
            queryProcessor.Execute(new DisposableHandlerQuery());
            queryProcessor.Execute(new DisposableHandlerQuery());

            // Assert — the container's single handler instance serves both queries and Darker never disposes it
            log.Handlers.Count.ShouldBe(1);
            log.Handlers[0].IsDisposed.ShouldBeFalse();
        }

        [Fact]
        public async Task When_executing_two_async_queries_should_use_one_handler_and_not_dispose_it()
        {
            // Arrange
            var provider = Build();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var log = provider.GetRequiredService<DisposableHandlerLog>();

            // Act
            await queryProcessor.ExecuteAsync(new DisposableHandlerQuery(), cancellationToken: TestContext.Current.CancellationToken);
            await queryProcessor.ExecuteAsync(new DisposableHandlerQuery(), cancellationToken: TestContext.Current.CancellationToken);

            // Assert — the container's single handler instance serves both queries and Darker never disposes it
            log.Handlers.Count.ShouldBe(1);
            log.Handlers[0].IsDisposed.ShouldBeFalse();
        }

        [Fact]
        public async Task When_executing_two_stream_queries_should_use_one_handler_and_not_dispose_it()
        {
            // Arrange
            var provider = Build();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var log = provider.GetRequiredService<DisposableHandlerLog>();

            // Act — enumerate each stream to completion
            await foreach (var _ in queryProcessor.ExecuteStream(new DisposableHandlerStreamQuery(), cancellationToken: TestContext.Current.CancellationToken)) { }
            await foreach (var _ in queryProcessor.ExecuteStream(new DisposableHandlerStreamQuery(), cancellationToken: TestContext.Current.CancellationToken)) { }

            // Assert — the container's single handler instance serves both queries and Darker never disposes it
            log.Handlers.Count.ShouldBe(1);
            log.Handlers[0].IsDisposed.ShouldBeFalse();
        }

        private static ServiceProvider Build()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddSingleton<DisposableHandlerLog>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(registry => registry.Register<DisposableHandlerQuery, string, DisposableTrackedHandler>())
                .AddAsyncHandlers(registry => registry.Register<DisposableHandlerQuery, string, DisposableTrackedHandlerAsync>())
                .AddStreamHandlers(registry => registry.Register<DisposableHandlerStreamQuery, string, DisposableTrackedStreamHandler>());

            return services.BuildServiceProvider();
        }
    }
}
