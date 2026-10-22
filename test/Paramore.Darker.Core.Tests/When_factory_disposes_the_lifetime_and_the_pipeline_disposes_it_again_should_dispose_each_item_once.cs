using System.Collections.Generic;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class LifetimeIdempotentDisposalTests
    {
        [Fact]
        public void When_factory_disposes_the_lifetime_and_the_pipeline_disposes_it_again_should_dispose_each_item_once()
        {
            // Arrange
            var recorder = new RecordingScope();

            var registry = new QueryHandlerRegistry();
            registry.Register<LifetimeIsolationQuery, LifetimeIsolationResult, LifetimeIsolationQueryHandler>();

            // The factory disposes the lifetime in Release; PipelineBuilder then disposes it again as its last step
            var handlerFactory = new SelfDisposingLifetimeHandlerFactory(_ => new LifetimeIsolationQueryHandler(), recorder);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);

            var config = new HandlerConfiguration(
                registry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new StreamQueryHandlerRegistry());

            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            // Act
            var result = processor.Execute(new LifetimeIsolationQuery());

            // Assert
            result.Value.ShouldBe(LifetimeIsolationQueryHandler.RESULT);
            recorder.DisposeCount.ShouldBe(1);
        }
    }
}
