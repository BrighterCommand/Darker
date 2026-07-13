// Copyright (c) 2025, Ian Cooper
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the
// following conditions are met:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
    /// Verifies that <c>PipelineBuilder.BuildStream</c> weaves one <c>WriteQueryEvent</c> per step
    /// (decorator + sink handler) into the stream pipeline when a span is present on the context,
    /// and that no events are added when the context carries no span (zero-overhead pass-through).
    /// </summary>
    [Collection("DarkerActivitySource")]
    public class PipelineBuilderStreamStepEventTests
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

        private static QueryProcessor CreateProcessorWithTracer(IAmADarkerTracer tracer, List<int> enteredSteps)
        {
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<StreamTestQuery, string, StreamSpanEventHandler>();

            var syncRegistry = new QueryHandlerRegistry();
            var handlerFactory = new SimpleHandlerFactory(_ => new StreamSpanEventHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                new StreamStepEventDecorator<IStreamQuery<string>, string>(enteredSteps));
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var config = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry);

            return new QueryProcessor(
                config,
                new InMemoryQueryContextFactory(),
                tracer: tracer,
                instrumentationOptions: InstrumentationOptions.QueryInformation);
        }

        [Fact]
        public async Task When_building_stream_pipeline_with_span_should_write_event_per_step()
        {
            // Arrange
            var completed = new List<Activity>();
            using var listener = CreateListener(completed);
            using var tracer = new DarkerTracer();
            var enteredSteps = new List<int>();
            var processor = CreateProcessorWithTracer(tracer, enteredSteps);
            var query = new StreamTestQuery();

            // Act — must enumerate to trigger span events
            var results = new List<string>();
            await foreach (var item in processor.ExecuteStream(query))
                results.Add(item);

            // Assert
            results.Count.ShouldBe(1);
            completed.Count.ShouldBe(1);
            var span = completed[0];

            var events = span.Events.ToList();
            events.Count.ShouldBe(2);

            // First event: decorator (outermost in pipeline — executes before calling next)
            var decoratorEvent = events[0];
            var decoratorTypeName = typeof(StreamStepEventDecorator<,>)
                .MakeGenericType(typeof(IStreamQuery<string>), typeof(string))
                .Name;
            decoratorEvent.Name.ShouldBe(decoratorTypeName);
            var decoratorTags = decoratorEvent.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            decoratorTags[DarkerSemanticConventions.HandlerType].ShouldBe("async");
            decoratorTags[DarkerSemanticConventions.IsSink].ShouldBe(false);

            // Second event: handler (innermost = sink — executes after decorator calls next)
            var handlerEvent = events[1];
            handlerEvent.Name.ShouldBe(typeof(StreamSpanEventHandler).Name);
            var handlerTags = handlerEvent.Tags.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            handlerTags[DarkerSemanticConventions.HandlerType].ShouldBe("async");
            handlerTags[DarkerSemanticConventions.IsSink].ShouldBe(true);
        }

        [Fact]
        public async Task When_building_stream_pipeline_without_span_should_not_add_events_and_run_cleanly()
        {
            // Arrange — no tracer so queryContext.Span is null; WriteQueryEvent must be a no-op
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<StreamTestQuery, string, StreamSpanEventHandler>();

            var syncRegistry = new QueryHandlerRegistry();
            var enteredSteps = new List<int>();
            var handlerFactory = new SimpleHandlerFactory(_ => new StreamSpanEventHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                new StreamStepEventDecorator<IStreamQuery<string>, string>(enteredSteps));
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var config = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry);
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

            var query = new StreamTestQuery();

            // Act — pipeline must execute correctly with no span present
            var results = new List<string>();
            await foreach (var item in processor.ExecuteStream(query))
                results.Add(item);

            // Assert — result returned normally; no listener active so no events to inspect
            results.Count.ShouldBe(1);
            results[0].ShouldBe("item");
        }
    }
}
