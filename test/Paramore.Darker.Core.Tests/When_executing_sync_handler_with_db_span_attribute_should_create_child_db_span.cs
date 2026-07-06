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
    /// <summary>
    /// Verifies that a sync handler decorated with <c>[QueryDbSpan]</c> produces a child DB span
    /// nested under the query span, with the correct <c>db.*</c> tags, and that the handler
    /// executes normally when no tracer is configured (zero-overhead path).
    /// </summary>
    [Collection("DarkerActivitySource")]
    public class QueryDbSpanDecoratorSyncTests
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
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithDbSpanAttribute>();

            var handlerFactory = new SimpleHandlerFactory(_ => new SyncHandlerWithDbSpanAttribute());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                new QueryDbSpanDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>());
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var config = new HandlerConfiguration(registry, handlerFactory, decoratorRegistry, decoratorFactory);
            return new QueryProcessor(
                config,
                new InMemoryQueryContextFactory(),
                tracer: tracer,
                instrumentationOptions: InstrumentationOptions.QueryInformation | InstrumentationOptions.DatabaseInformation);
        }

        [Fact]
        public void When_executing_sync_handler_with_db_span_attribute_should_create_child_db_span()
        {
            // Arrange
            var completed = new List<Activity>();
            using var listener = CreateListener(completed);
            using var tracer = new DarkerTracer();
            var processor = CreateProcessorWithTracer(tracer);
            var query = new SyncTestQuery(Guid.NewGuid());

            // Act
            var result = processor.Execute(query);

            // Assert — result returned normally
            result.Value.ShouldBe(query.Id);

            // Assert — 2 spans stopped: DB span (Client) + query span (Internal)
            completed.Count.ShouldBe(2);
            var querySpan = completed.First(a => a.Kind == ActivityKind.Internal);
            var dbSpan = completed.First(a => a.Kind == ActivityKind.Client);

            // DB span is nested under the query span
            dbSpan.ParentId.ShouldBe(querySpan.Id);

            // DB span has db.* tags
            dbSpan.GetTagItem(DarkerSemanticConventions.DbSystem).ShouldBe("postgresql");
            dbSpan.GetTagItem(DarkerSemanticConventions.DbName).ShouldBe("orders");
            dbSpan.GetTagItem(DarkerSemanticConventions.DbOperation).ShouldBe("select");

            // DB span is stopped (present in completed list) after Execute returns — already verified above
            // Confirm kind is Client, as required by OTel DB span conventions
            dbSpan.Kind.ShouldBe(ActivityKind.Client);
        }

        [Fact]
        public void When_no_tracer_configured_handler_runs_unchanged_with_no_db_span()
        {
            // Arrange — listener active but no tracer on the processor
            var completed = new List<Activity>();
            using var listener = CreateListener(completed);
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, SyncHandlerWithDbSpanAttribute>();
            var handlerFactory = new SimpleHandlerFactory(_ => new SyncHandlerWithDbSpanAttribute());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                new QueryDbSpanDecorator<IQuery<SyncTestQuery.Result>, SyncTestQuery.Result>());
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var config = new HandlerConfiguration(registry, handlerFactory, decoratorRegistry, decoratorFactory);
            var processor = new QueryProcessor(config, new InMemoryQueryContextFactory()); // no tracer
            var query = new SyncTestQuery(Guid.NewGuid());

            // Act — must not throw even though Context.Tracer is null
            var result = processor.Execute(query);

            // Assert — result is returned correctly and no spans were created
            result.Value.ShouldBe(query.Id);
            completed.Count.ShouldBe(0);
        }
    }
}
