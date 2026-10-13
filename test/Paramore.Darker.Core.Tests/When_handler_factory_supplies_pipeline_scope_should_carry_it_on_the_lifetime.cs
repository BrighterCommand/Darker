using System;
using System.Linq;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_handler_factory_supplies_pipeline_scope_should_carry_it_on_the_lifetime
    {
        private readonly ScopeSupplyingHandlerFactory _handlerFactory = new ScopeSupplyingHandlerFactory();
        private readonly LifetimeRecordingDecoratorFactory _decoratorFactory = new LifetimeRecordingDecoratorFactory();

        [Fact]
        public void When_executing_should_pass_a_lifetime_carrying_the_handle_to_the_handler_and_decorator()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register<PipelineScopeQuery, string, PipelineScopeQueryHandler>();
            var processor = BuildProcessor(_handlerFactory, _decoratorFactory, registry: registry);

            // Act
            var result = processor.Execute(new PipelineScopeQuery());

            // Assert
            result.ShouldBe(PipelineScopeQueryHandler.RESULT);
            AssertOneHandleOnTheSharedLifetime();
        }

        [Fact]
        public async Task When_executing_async_should_pass_a_lifetime_carrying_the_handle_to_the_handler_and_decorator()
        {
            // Arrange
            var registryAsync = new QueryHandlerRegistryAsync();
            registryAsync.Register<PipelineScopeAsyncQuery, string, PipelineScopeAsyncQueryHandler>();
            var processor = BuildProcessor(_handlerFactory, _decoratorFactory, registryAsync: registryAsync);

            // Act
            var result = await processor.ExecuteAsync(new PipelineScopeAsyncQuery());

            // Assert
            result.ShouldBe(PipelineScopeAsyncQueryHandler.RESULT);
            AssertOneHandleOnTheSharedLifetime();
        }

        [Fact]
        public async Task When_executing_stream_should_pass_a_lifetime_carrying_the_handle_to_the_handler_and_decorator()
        {
            // Arrange
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<PipelineScopeStreamQuery, string, PipelineScopeStreamQueryHandler>();
            var processor = BuildProcessor(_handlerFactory, _decoratorFactory, streamRegistry: streamRegistry);

            // Act
            var items = new System.Collections.Generic.List<string>();
            await foreach (var item in processor.ExecuteStream(new PipelineScopeStreamQuery()))
                items.Add(item);

            // Assert
            items.ShouldBe(new[] { PipelineScopeStreamQueryHandler.ITEM });
            AssertOneHandleOnTheSharedLifetime();
        }

        [Fact]
        public void When_a_simple_handler_factory_is_used_on_execute_should_give_the_decorator_factory_no_handle()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register<PipelineScopeQuery, string, PipelineScopeQueryHandler>();
            var processor = BuildProcessor(SimpleHandlers(), _decoratorFactory, registry: registry);

            // Act
            var result = processor.Execute(new PipelineScopeQuery());

            // Assert
            result.ShouldBe(PipelineScopeQueryHandler.RESULT);
            AssertDecoratorFactorySawNoHandle();
        }

        [Fact]
        public async Task When_a_simple_handler_factory_is_used_on_execute_async_should_give_the_decorator_factory_no_handle()
        {
            // Arrange
            var registryAsync = new QueryHandlerRegistryAsync();
            registryAsync.Register<PipelineScopeAsyncQuery, string, PipelineScopeAsyncQueryHandler>();
            var processor = BuildProcessor(SimpleHandlers(), _decoratorFactory, registryAsync: registryAsync);

            // Act
            var result = await processor.ExecuteAsync(new PipelineScopeAsyncQuery());

            // Assert
            result.ShouldBe(PipelineScopeAsyncQueryHandler.RESULT);
            AssertDecoratorFactorySawNoHandle();
        }

        [Fact]
        public async Task When_a_simple_handler_factory_is_used_on_execute_stream_should_give_the_decorator_factory_no_handle()
        {
            // Arrange
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<PipelineScopeStreamQuery, string, PipelineScopeStreamQueryHandler>();
            var processor = BuildProcessor(SimpleHandlers(), _decoratorFactory, streamRegistry: streamRegistry);

            // Act
            var items = new System.Collections.Generic.List<string>();
            await foreach (var item in processor.ExecuteStream(new PipelineScopeStreamQuery()))
                items.Add(item);

            // Assert
            items.ShouldBe(new[] { PipelineScopeStreamQueryHandler.ITEM });
            AssertDecoratorFactorySawNoHandle();
        }

        [Fact]
        public void When_simple_factories_are_used_on_execute_should_still_run_the_query()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register<PipelineScopeQuery, string, PipelineScopeQueryHandler>();
            var processor = BuildProcessor(SimpleHandlers(), SimpleDecorators(), registry: registry);

            // Act
            var result = processor.Execute(new PipelineScopeQuery());

            // Assert
            result.ShouldBe(PipelineScopeQueryHandler.RESULT);
        }

        [Fact]
        public async Task When_simple_factories_are_used_on_execute_async_should_still_run_the_query()
        {
            // Arrange
            var registryAsync = new QueryHandlerRegistryAsync();
            registryAsync.Register<PipelineScopeAsyncQuery, string, PipelineScopeAsyncQueryHandler>();
            var processor = BuildProcessor(SimpleHandlers(), SimpleDecorators(), registryAsync: registryAsync);

            // Act
            var result = await processor.ExecuteAsync(new PipelineScopeAsyncQuery());

            // Assert
            result.ShouldBe(PipelineScopeAsyncQueryHandler.RESULT);
        }

        [Fact]
        public async Task When_simple_factories_are_used_on_execute_stream_should_still_run_the_query()
        {
            // Arrange
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<PipelineScopeStreamQuery, string, PipelineScopeStreamQueryHandler>();
            var processor = BuildProcessor(SimpleHandlers(), SimpleDecorators(), streamRegistry: streamRegistry);

            // Act
            var items = new System.Collections.Generic.List<string>();
            await foreach (var item in processor.ExecuteStream(new PipelineScopeStreamQuery()))
                items.Add(item);

            // Assert
            items.ShouldBe(new[] { PipelineScopeStreamQueryHandler.ITEM });
        }

        private void AssertOneHandleOnTheSharedLifetime()
        {
            // The factory was asked for exactly one handle, and that handle is on the lifetime
            // that both the handler factory and the decorator factory received.
            _handlerFactory.ScopesCreated.Count.ShouldBe(1);
            var handle = _handlerFactory.ScopesCreated.Single();

            _handlerFactory.LifetimesSeen.Count.ShouldBe(1);
            _decoratorFactory.LifetimesSeen.Count.ShouldBe(1);

            var lifetime = _handlerFactory.LifetimesSeen.Single();
            _decoratorFactory.LifetimesSeen.Single().ShouldBeSameAs(lifetime);
            lifetime.PipelineScope.ShouldBeSameAs(handle);
        }

        private void AssertDecoratorFactorySawNoHandle()
        {
            _decoratorFactory.LifetimesSeen.Count.ShouldBe(1);
            _decoratorFactory.LifetimesSeen.Single().PipelineScope.ShouldBeNull();
        }

        private static SimpleHandlerFactory SimpleHandlers()
            => new SimpleHandlerFactory(type => (IQueryHandler)Activator.CreateInstance(type));

        private static SimpleHandlerDecoratorFactory SimpleDecorators()
            => new SimpleHandlerDecoratorFactory(type => (IQueryHandlerDecorator)Activator.CreateInstance(type));

        private static QueryProcessor BuildProcessor(
            object handlerFactory,
            object decoratorFactory,
            QueryHandlerRegistry registry = null,
            QueryHandlerRegistryAsync registryAsync = null,
            StreamQueryHandlerRegistry streamRegistry = null)
        {
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var config = new HandlerConfiguration(
                registry ?? new QueryHandlerRegistry(), (IQueryHandlerFactory)handlerFactory,
                decoratorRegistry, (IQueryHandlerDecoratorFactory)decoratorFactory,
                registryAsync ?? new QueryHandlerRegistryAsync(), (IQueryHandlerFactoryAsync)handlerFactory,
                decoratorRegistry, (IQueryHandlerDecoratorFactoryAsync)decoratorFactory,
                streamRegistry ?? new StreamQueryHandlerRegistry());

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }
    }
}
