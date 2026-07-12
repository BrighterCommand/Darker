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
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    [Collection("DarkerActivitySource")]
    public class When_stream_consumer_breaks_early_should_release_pipeline_and_end_span_once
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

        [Fact]
        public async Task When_stream_consumer_breaks_early_should_release_handler_and_end_span_exactly_once()
        {
            // Arrange — recording factory tracks which handlers were released
            var completed = new List<Activity>();
            using var listener = CreateListener(completed);
            using var tracer = new DarkerTracer();

            IQueryHandler createdHandler = null;
            var handlerFactory = new RecordingHandlerFactory(t =>
            {
                createdHandler = new MultiItemStreamHandler();
                return createdHandler;
            });

            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<MultiItemStreamQuery, string, MultiItemStreamHandler>();

            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, decoratorRegistry, decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry);

            var processor = new QueryProcessor(
                config,
                new InMemoryQueryContextFactory(),
                tracer: tracer,
                instrumentationOptions: InstrumentationOptions.QueryInformation);

            // Act — break after the first item (early consumer exit)
            var receivedItems = new List<string>();
            await foreach (var item in processor.ExecuteStream(new MultiItemStreamQuery()))
            {
                receivedItems.Add(item);
                break;
            }

            // Assert — handler released exactly once via the try/finally in ExecuteStream
            createdHandler.ShouldNotBeNull();
            handlerFactory.ReleaseCount(createdHandler).ShouldBe(1,
                "the ExecuteStream finally block must release the handler exactly once on early break");

            // Assert — span ended exactly once (inner finally calls EndSpan, outer finally disposes pipeline)
            completed.Count.ShouldBe(1, "the span must be ended exactly once when the consumer breaks early");

            receivedItems.Count.ShouldBe(1);
            receivedItems[0].ShouldBe(MultiItemStreamHandler.Items[0]);
        }
    }
}
