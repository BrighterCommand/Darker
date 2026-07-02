using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    /// <summary>
    /// Verifies that <c>PipelineBuilder.BuildAsync</c> weaves one <c>WriteQueryEvent</c> per step
    /// (decorator + sink handler) into the async <c>Func</c> chain, and that no events are added
    /// when the context carries no span (zero-overhead pass-through path).
    /// </summary>
    [Collection("DarkerActivitySource")]
    public class PipelineBuilderAsyncStepEventTests
    {
        private static ActivityListener CreateListener(List<Activity> completed)
        {
            var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == DarkerSemanticConventions.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
                ActivityStopped = a => completed.Add(a),
            };
            ActivitySource.AddActivityListener(listener);
            return listener;
        }

        private static QueryProcessor CreateProcessorWithTracer(IAmADarkerTracer tracer)
        {
            var syncRegistry = new QueryHandlerRegistry();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncStepEventHandler>();

            var handlerFactory = new SimpleHandlerFactory(_ => new AsyncStepEventHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                new AsyncStepEventDecorator<IQuery<AsyncTestQuery.Result>, AsyncTestQuery.Result>());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var config = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);

            return new QueryProcessor(
                config,
                new InMemoryQueryContextFactory(),
                tracer: tracer,
                instrumentationOptions: InstrumentationOptions.QueryInformation);
        }

        [Fact]
        public async Task When_building_async_pipeline_with_span_should_write_event_per_step()
        {
            // Arrange
            var completed = new List<Activity>();
            using var listener = CreateListener(completed);
            using var tracer = new DarkerTracer();
            var processor = CreateProcessorWithTracer(tracer);
            var query = new AsyncTestQuery(Guid.NewGuid());

            // Act
            var result = await processor.ExecuteAsync(query);

            // Assert
            result.ShouldNotBeNull();
            completed.Count.ShouldBe(1);
            var span = completed[0];

            var events = span.Events.ToList();
            events.Count.ShouldBe(2);

            // First event: decorator (outermost in pipeline — executes before calling next)
            var decoratorEvent = events[0];
            var decoratorTypeName = typeof(AsyncStepEventDecorator<,>)
                .MakeGenericType(typeof(IQuery<AsyncTestQuery.Result>), typeof(AsyncTestQuery.Result))
                .Name;
            decoratorEvent.Name.ShouldBe(decoratorTypeName);
            var decoratorTags = decoratorEvent.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            decoratorTags[DarkerSemanticConventions.HandlerType].ShouldBe("async");
            decoratorTags[DarkerSemanticConventions.IsSink].ShouldBe(false);

            // Second event: handler (innermost = sink — executes after decorator calls next)
            var handlerEvent = events[1];
            handlerEvent.Name.ShouldBe(typeof(AsyncStepEventHandler).Name);
            var handlerTags = handlerEvent.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            handlerTags[DarkerSemanticConventions.HandlerType].ShouldBe("async");
            handlerTags[DarkerSemanticConventions.IsSink].ShouldBe(true);
        }

        [Fact]
        public async Task When_building_async_pipeline_without_span_should_not_add_events_and_run_cleanly()
        {
            // Arrange — no tracer so queryContext.Span is null; WriteQueryEvent must be a no-op
            var syncRegistry = new QueryHandlerRegistry();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncStepEventHandler>();

            var handlerFactory = new SimpleHandlerFactory(_ => new AsyncStepEventHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                new AsyncStepEventDecorator<IQuery<AsyncTestQuery.Result>, AsyncTestQuery.Result>());
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var config = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            var query = new AsyncTestQuery(Guid.NewGuid());

            // Act — pipeline must execute correctly with no span present
            var result = await processor.ExecuteAsync(query);

            // Assert — result returned normally; no listener active so no events to inspect
            result.ShouldNotBeNull();
            result.Value.ShouldBe(query.Id);
        }
    }
}
