using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Paramore.Darker.Observability.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    [Collection("DarkerActivitySource")]
    public class QueryDbSpanAmbientActivityTests
    {
        [Fact]
        public void When_a_db_span_step_ends_should_leave_the_query_span_current_for_outer_decorators()
        {
            // Arrange — an outer decorator (step 1) wraps the [QueryDbSpan] step (step 2) and records Activity.Current after next()
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
            var outer = new CurrentActivityCapturingDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>();
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithDbSpanInsideCapturingDecorator>();
            var handlerFactory = new SimpleHandlerFactory(_ => new SyncHandlerWithDbSpanInsideCapturingDecorator());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(type =>
                type.GetGenericTypeDefinition() == typeof(CurrentActivityCapturingDecorator<,>)
                    ? outer
                    : new QueryDbSpanDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>());
            var config = new HandlerConfiguration(registry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory);
            var processor = new QueryProcessor(
                config,
                new InMemoryQueryContextFactory(),
                tracer: tracer,
                instrumentationOptions: InstrumentationOptions.QueryInformation | InstrumentationOptions.DatabaseInformation);

            try
            {
                // Act
                processor.Execute(new SyncTestQuery(Guid.NewGuid()));

                // Assert — after the DB span ended, the outer decorator still ran under the query span
                var querySpan = completed.Single(a => a.Kind == ActivityKind.Internal);
                outer.CurrentAfterNext.ShouldBeSameAs(querySpan);
            }
            finally
            {
                Activity.Current = null;
            }
        }
    }
}
