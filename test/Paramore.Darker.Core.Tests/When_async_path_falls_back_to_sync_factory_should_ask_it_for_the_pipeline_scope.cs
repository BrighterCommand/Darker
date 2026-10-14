using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_async_path_falls_back_to_sync_factory_should_ask_it_for_the_pipeline_scope
    {
        private readonly ScopeSupplyingHandlerFactory _syncFactory = new ScopeSupplyingHandlerFactory();
        private readonly ScopeSupplyingHandlerFactory _asyncFactory = new ScopeSupplyingHandlerFactory();
        private readonly LifetimeRecordingDecoratorFactory _decoratorFactory = new LifetimeRecordingDecoratorFactory();

        [Fact]
        public async Task When_there_is_no_async_registry_on_execute_async_should_ask_only_the_sync_factory()
        {
            // Arrange — an async handler factory is configured, but no async registry; the handler is in the sync registry
            var registry = new QueryHandlerRegistry();
            registry.Register<PipelineScopeAsyncQuery, string, DualPipelineScopeQueryHandler>();
            var processor = BuildProcessor(registry: registry, registryAsync: null, streamRegistry: null, withAsyncFactory: true);

            // Act
            var result = await processor.ExecuteAsync(new PipelineScopeAsyncQuery());

            // Assert
            result.ShouldBe(DualPipelineScopeQueryHandler.RESULT);
            _asyncFactory.ScopesCreated.Count.ShouldBe(0);
            _syncFactory.ScopesCreated.Count.ShouldBe(1);
        }

        [Fact]
        public async Task When_there_is_no_async_factory_on_execute_stream_should_ask_the_sync_factory()
        {
            // Arrange — no async factory at all; the stream handler is created by the sync factory
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<PipelineScopeStreamQuery, string, PipelineScopeStreamQueryHandler>();
            var processor = BuildProcessor(registry: new QueryHandlerRegistry(), registryAsync: null,
                streamRegistry: streamRegistry, withAsyncFactory: false);

            // Act
            var items = new List<string>();
            await foreach (var item in processor.ExecuteStream(new PipelineScopeStreamQuery()))
                items.Add(item);

            // Assert
            items.ShouldBe(new[] { PipelineScopeStreamQueryHandler.ITEM });
            _syncFactory.ScopesCreated.Count.ShouldBe(1);
        }

        [Fact]
        public async Task When_both_async_registry_and_factory_are_configured_on_execute_async_should_ask_only_the_async_factory()
        {
            // Arrange
            var registryAsync = new QueryHandlerRegistryAsync();
            registryAsync.Register<PipelineScopeAsyncQuery, string, PipelineScopeAsyncQueryHandler>();
            var processor = BuildProcessor(registry: new QueryHandlerRegistry(), registryAsync: registryAsync,
                streamRegistry: null, withAsyncFactory: true);

            // Act
            var result = await processor.ExecuteAsync(new PipelineScopeAsyncQuery());

            // Assert
            result.ShouldBe(PipelineScopeAsyncQueryHandler.RESULT);
            _asyncFactory.ScopesCreated.Count.ShouldBe(1);
            _syncFactory.ScopesCreated.Count.ShouldBe(0);
        }

        private QueryProcessor BuildProcessor(
            QueryHandlerRegistry registry,
            QueryHandlerRegistryAsync registryAsync,
            StreamQueryHandlerRegistry streamRegistry,
            bool withAsyncFactory)
        {
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var config = new HandlerConfiguration(
                registry, _syncFactory, decoratorRegistry, _decoratorFactory,
                registryAsync, withAsyncFactory ? _asyncFactory : null, decoratorRegistry, _decoratorFactory,
                streamRegistry);

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }
    }
}
