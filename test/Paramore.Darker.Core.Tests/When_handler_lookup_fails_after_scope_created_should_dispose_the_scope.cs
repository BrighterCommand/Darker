using System.Linq;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_handler_lookup_fails_after_scope_created_should_dispose_the_scope
    {
        private readonly ScopeSupplyingHandlerFactory _handlerFactory = new ScopeSupplyingHandlerFactory();

        [Fact]
        public void When_no_sync_handler_is_registered_on_execute_should_surface_the_lookup_exception_and_dispose_the_scope_once()
        {
            // Arrange — the factory supplies a scope, but no handler is registered for the query
            var processor = BuildProcessor();

            // Act
            Should.Throw<ConfigurationException>(() => processor.Execute(new PipelineScopeQuery()));

            // Assert
            var scope = _handlerFactory.ScopesCreated.Single();
            scope.DisposeCount.ShouldBe(1);
            scope.DisposeAsyncCount.ShouldBe(0);
        }

        [Fact]
        public async Task When_no_async_handler_is_registered_on_execute_async_should_surface_the_lookup_exception_and_dispose_the_scope_once()
        {
            // Arrange
            var processor = BuildProcessor();

            // Act
            await Should.ThrowAsync<ConfigurationException>(() => processor.ExecuteAsync(new PipelineScopeAsyncQuery()));

            // Assert
            var scope = _handlerFactory.ScopesCreated.Single();
            scope.DisposeAsyncCount.ShouldBe(1);
            scope.DisposeCount.ShouldBe(0);
        }

        [Fact]
        public async Task When_no_stream_handler_is_registered_on_execute_stream_should_surface_the_lookup_exception_and_dispose_the_scope_once()
        {
            // Arrange
            var processor = BuildProcessor();

            // Act
            await Should.ThrowAsync<ConfigurationException>(async () =>
            {
                await foreach (var _ in processor.ExecuteStream(new PipelineScopeStreamQuery()))
                {
                }
            });

            // Assert
            var scope = _handlerFactory.ScopesCreated.Single();
            scope.DisposeAsyncCount.ShouldBe(1);
            scope.DisposeCount.ShouldBe(0);
        }

        private QueryProcessor BuildProcessor()
        {
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var decoratorFactory = new LifetimeRecordingDecoratorFactory();
            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), _handlerFactory, decoratorRegistry, decoratorFactory,
                new QueryHandlerRegistryAsync(), _handlerFactory, decoratorRegistry, decoratorFactory,
                new StreamQueryHandlerRegistry());

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }
    }
}
