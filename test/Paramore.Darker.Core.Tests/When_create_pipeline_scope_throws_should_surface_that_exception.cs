using System;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_create_pipeline_scope_throws_should_surface_that_exception
    {
        private readonly InvalidOperationException _fault = new InvalidOperationException("the container could not create a scope");
        private readonly ThrowingScopeHandlerFactory _handlerFactory;
        private readonly LifetimeRecordingDecoratorFactory _decoratorFactory = new LifetimeRecordingDecoratorFactory();

        public When_create_pipeline_scope_throws_should_surface_that_exception()
        {
            _handlerFactory = new ThrowingScopeHandlerFactory(_fault);
        }

        [Fact]
        public void When_executing_should_surface_the_exception_and_create_and_release_nothing()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register<PipelineScopeQuery, string, PipelineScopeQueryHandler>();
            var processor = BuildProcessor(registry: registry);

            // Act
            var thrown = Should.Throw<InvalidOperationException>(() => processor.Execute(new PipelineScopeQuery()));

            // Assert
            thrown.ShouldBeSameAs(_fault);
            AssertNothingCreatedOrReleased();
        }

        [Fact]
        public async Task When_executing_async_should_surface_the_exception_and_create_and_release_nothing()
        {
            // Arrange
            var registryAsync = new QueryHandlerRegistryAsync();
            registryAsync.Register<PipelineScopeAsyncQuery, string, PipelineScopeAsyncQueryHandler>();
            var processor = BuildProcessor(registryAsync: registryAsync);

            // Act
            var thrown = await Should.ThrowAsync<InvalidOperationException>(
                () => processor.ExecuteAsync(new PipelineScopeAsyncQuery()));

            // Assert
            thrown.ShouldBeSameAs(_fault);
            AssertNothingCreatedOrReleased();
        }

        [Fact]
        public async Task When_executing_stream_should_surface_the_exception_at_the_first_move_next_and_create_and_release_nothing()
        {
            // Arrange
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<PipelineScopeStreamQuery, string, PipelineScopeStreamQueryHandler>();
            var processor = BuildProcessor(streamRegistry: streamRegistry);
            var enumerator = processor.ExecuteStream(new PipelineScopeStreamQuery()).GetAsyncEnumerator();

            // Act
            var thrown = await Should.ThrowAsync<InvalidOperationException>(async () => await enumerator.MoveNextAsync());

            // Assert
            thrown.ShouldBeSameAs(_fault);
            AssertNothingCreatedOrReleased();
        }

        private void AssertNothingCreatedOrReleased()
        {
            _handlerFactory.CreateCount.ShouldBe(0);
            _handlerFactory.ReleaseCount.ShouldBe(0);
            _decoratorFactory.LifetimesSeen.ShouldBeEmpty();
        }

        private QueryProcessor BuildProcessor(
            QueryHandlerRegistry registry = null,
            QueryHandlerRegistryAsync registryAsync = null,
            StreamQueryHandlerRegistry streamRegistry = null)
        {
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var config = new HandlerConfiguration(
                registry ?? new QueryHandlerRegistry(), _handlerFactory, decoratorRegistry, _decoratorFactory,
                registryAsync ?? new QueryHandlerRegistryAsync(), _handlerFactory, decoratorRegistry, _decoratorFactory,
                streamRegistry ?? new StreamQueryHandlerRegistry());

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }
    }
}
