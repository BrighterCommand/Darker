using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class PipelineScopeDisposalOrderTests
    {
        private readonly TeardownEventLog _log = new TeardownEventLog();

        [Fact]
        public void When_executing_should_dispose_the_scope_once_as_the_last_teardown_event()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register<PipelineScopeQuery, string, PipelineScopeQueryHandler>();
            var processor = BuildProcessor(registry: registry);

            // Act
            var result = processor.Execute(new PipelineScopeQuery());

            // Assert
            result.ShouldBe(PipelineScopeQueryHandler.RESULT);
            _log.Events.ShouldBe(new[]
            {
                TeardownEventLog.HANDLER_RELEASED,
                TeardownEventLog.DECORATOR_RELEASED,
                TeardownEventLog.SCOPE_DISPOSE
            });
        }

        [Fact]
        public async Task When_executing_async_should_await_dispose_async_once_as_the_last_teardown_event()
        {
            // Arrange
            var registryAsync = new QueryHandlerRegistryAsync();
            registryAsync.Register<PipelineScopeAsyncQuery, string, PipelineScopeAsyncQueryHandler>();
            var processor = BuildProcessor(registryAsync: registryAsync);

            // Act
            var result = await processor.ExecuteAsync(new PipelineScopeAsyncQuery(), cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            result.ShouldBe(PipelineScopeAsyncQueryHandler.RESULT);
            AssertScopeDisposedAsyncLast();
        }

        [Fact]
        public async Task When_executing_stream_should_await_dispose_async_once_as_the_last_teardown_event()
        {
            // Arrange
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<PipelineScopeStreamQuery, string, PipelineScopeStreamQueryHandler>();
            var processor = BuildProcessor(streamRegistry: streamRegistry);

            // Act
            var items = new List<string>();
            await foreach (var item in processor.ExecuteStream(new PipelineScopeStreamQuery(), cancellationToken: TestContext.Current.CancellationToken))
                items.Add(item);

            // Assert
            items.ShouldBe(new[] { PipelineScopeStreamQueryHandler.ITEM });
            AssertScopeDisposedAsyncLast();
        }

        private void AssertScopeDisposedAsyncLast()
        {
            // Both components are released first, then the handle is disposed asynchronously, once,
            // and its synchronous Dispose is never called.
            _log.Events.ShouldBe(new[]
            {
                TeardownEventLog.HANDLER_RELEASED,
                TeardownEventLog.DECORATOR_RELEASED,
                TeardownEventLog.SCOPE_DISPOSE_ASYNC
            });
        }

        private QueryProcessor BuildProcessor(
            QueryHandlerRegistry registry = null,
            QueryHandlerRegistryAsync registryAsync = null,
            StreamQueryHandlerRegistry streamRegistry = null)
        {
            var handlerFactory = new ScopeSupplyingHandlerFactory(_log);
            var decoratorFactory = new LifetimeRecordingDecoratorFactory(_log);
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var config = new HandlerConfiguration(
                registry ?? new QueryHandlerRegistry(), handlerFactory,
                decoratorRegistry, decoratorFactory,
                registryAsync ?? new QueryHandlerRegistryAsync(), handlerFactory,
                decoratorRegistry, decoratorFactory,
                streamRegistry ?? new StreamQueryHandlerRegistry());

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }
    }
}
