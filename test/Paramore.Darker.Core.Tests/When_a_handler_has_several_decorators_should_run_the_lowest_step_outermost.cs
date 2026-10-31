using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class DecoratorStepOrderTests
    {
        [Fact]
        public void When_a_handler_has_several_decorators_should_run_the_lowest_step_outermost()
        {
            // Arrange — the handler declares Steps 2, 3, 1 in that order, so source order cannot explain the result
            var log = new List<string>();
            var registry = new QueryHandlerRegistry();
            registry.Register<StepOrderRecordingQuery, string, StepOrderRecordingQueryHandler>();
            var handlerFactory = new SimpleHandlerFactory(_ => new StepOrderRecordingQueryHandler(log));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                new StepOrderRecordingDecorator<IQuery<string>, string>(log));
            var configuration = new HandlerConfiguration(
                registry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory);
            var queryProcessor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());

            // Act
            queryProcessor.Execute(new StepOrderRecordingQuery());

            // Assert — the lowest Step is entered first (outermost); the handler runs last
            log.ShouldBe(["step 1", "step 2", "step 3", "handler"]);
        }

        [Fact]
        public async Task When_an_async_handler_has_several_decorators_should_run_the_lowest_step_outermost()
        {
            // Arrange — the handler declares Steps 2, 3, 1 in that order, so source order cannot explain the result
            var log = new List<string>();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<StepOrderRecordingAsyncQuery, string, StepOrderRecordingQueryHandlerAsync>();
            var handlerFactory = new SimpleHandlerFactory(_ => new StepOrderRecordingQueryHandlerAsync(log));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                new StepOrderRecordingAsyncDecorator<IQuery<string>, string>(log));
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var configuration = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);
            var queryProcessor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());

            // Act
            await queryProcessor.ExecuteAsync(new StepOrderRecordingAsyncQuery(), cancellationToken: TestContext.Current.CancellationToken);

            // Assert — the lowest Step is entered first (outermost); the handler runs last
            log.ShouldBe(["step 1", "step 2", "step 3", "handler"]);
        }
    }
}
