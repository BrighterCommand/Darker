using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    [Collection("DarkerActivitySource")]
    public class StreamPipelineBuildFailureTracingTests
    {
        [Fact]
        public async Task When_the_stream_pipeline_fails_to_build_should_end_the_span_with_error_and_restore_activity_current()
        {
            // Arrange — no stream handler is registered for MultiItemStreamQuery, so building the pipeline throws on the first MoveNextAsync
            var completed = new List<Activity>();
            using var listener = new ActivityListener
            {
                ShouldListenTo = s => s.Name == DarkerSemanticConventions.SourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
                ActivityStopped = a => completed.Add(a),
            };
            ActivitySource.AddActivityListener(listener);
            using var tracer = new DarkerTracer();
            var handlerFactory = new SimpleHandlerFactory(_ => throw new InvalidOperationException("should not be called"));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => throw new InvalidOperationException("should not be called"));
            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new StreamQueryHandlerRegistry());
            var processor = new QueryProcessor(
                config,
                new InMemoryQueryContextFactory(),
                tracer: tracer,
                instrumentationOptions: InstrumentationOptions.QueryInformation);
            var callerActivity = new Activity("caller").Start();

            try
            {
                // Act
                await Should.ThrowAsync<ConfigurationException>(async () =>
                {
                    await foreach (var _ in processor.ExecuteStream(new MultiItemStreamQuery(), cancellationToken: TestContext.Current.CancellationToken)) { }
                });

                // Assert — the query span is stopped with the build failure recorded on it
                var span = completed.ShouldHaveSingleItem();
                span.Status.ShouldBe(ActivityStatusCode.Error);
                span.Events.Any(e => e.Name == "exception").ShouldBeTrue();
                span.GetTagItem(DarkerSemanticConventions.ErrorType).ShouldBe(nameof(ConfigurationException));

                // Assert — the caller's ambient activity is current again, not the query span
                Activity.Current.ShouldBeSameAs(callerActivity);
            }
            finally
            {
                // A leaked query span would otherwise stay current for later tests in the collection
                callerActivity.Stop();
                Activity.Current = null;
            }
        }
    }
}
