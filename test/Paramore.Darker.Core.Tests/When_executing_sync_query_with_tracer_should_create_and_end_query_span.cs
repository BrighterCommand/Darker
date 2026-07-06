using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    [Collection("DarkerActivitySource")]
    public class QueryProcessorSyncTracingTests
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

        private static QueryProcessor CreateProcessorWithTracer(
            IAmADarkerTracer tracer,
            IQueryHandlerRegistry registry,
            IQueryHandlerFactory factory)
        {
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => throw new NotImplementedException());
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var handlerConfig = new HandlerConfiguration(registry, factory, decoratorRegistry, decoratorFactory);
            return new QueryProcessor(
                handlerConfig,
                new InMemoryQueryContextFactory(),
                tracer: tracer,
                instrumentationOptions: InstrumentationOptions.QueryInformation);
        }

        [Fact]
        public void When_executing_sync_query_with_tracer_should_create_and_end_query_span()
        {
            // Arrange
            var completed = new List<Activity>();
            using var listener = CreateListener(completed);
            using var tracer = new DarkerTracer();
            var id = Guid.NewGuid();
            var handler = new ContextCapturingHandler();
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, ContextCapturingHandler>();
            var processor = CreateProcessorWithTracer(tracer, registry, new SimpleHandlerFactory(_ => handler));

            // Act
            var result = processor.Execute(new SyncTestQuery(id));

            // Assert
            result.Value.ShouldBe(id);
            completed.Count.ShouldBe(1);
            var span = completed[0];
            span.DisplayName.ShouldBe("SyncTestQuery query");
            span.Status.ShouldBe(ActivityStatusCode.Ok);
            handler.CapturedContext.Span.ShouldNotBeNull();
            handler.CapturedContext.Span.ShouldBeSameAs(span);
        }

        [Fact]
        public void When_throwing_handler_should_set_error_status_and_propagate_unwrapped_exception()
        {
            // Arrange
            var completed = new List<Activity>();
            using var listener = CreateListener(completed);
            using var tracer = new DarkerTracer();
            var id = Guid.NewGuid();
            var handler = new RecordingQueryHandler<SyncTestQuery, SyncTestQuery.Result>(
                _ => throw new InvalidOperationException("boom"));
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, RecordingQueryHandler<SyncTestQuery, SyncTestQuery.Result>>();
            var processor = CreateProcessorWithTracer(tracer, registry, new SimpleHandlerFactory(_ => handler));

            // Act — exception propagates unwrapped (not TargetInvocationException)
            var ex = Assert.Throws<InvalidOperationException>(() => processor.Execute(new SyncTestQuery(id)));

            // Assert — exception propagates with original message
            ex.Message.ShouldBe("boom");

            // Assert — span ended with Error status and recorded exception event
            completed.Count.ShouldBe(1);
            var span = completed[0];
            span.Status.ShouldBe(ActivityStatusCode.Error);
            span.Events.Any(e => e.Name == "exception").ShouldBeTrue();
        }

        [Fact]
        public void When_executing_sync_query_should_restore_Activity_Current_after_execution()
        {
            // Arrange
            var completed = new List<Activity>();
            using var listener = CreateListener(completed);
            using var tracer = new DarkerTracer();
            var id = Guid.NewGuid();
            var handler = new ContextCapturingHandler();
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, ContextCapturingHandler>();
            var processor = CreateProcessorWithTracer(tracer, registry, new SimpleHandlerFactory(_ => handler));
            var priorCurrent = Activity.Current;

            // Act
            processor.Execute(new SyncTestQuery(id));

            // Assert — Activity.Current is restored to its pre-call value
            Activity.Current.ShouldBe(priorCurrent);
        }

        [Fact]
        public void When_no_tracer_configured_should_not_create_span()
        {
            // Arrange — processor without a tracer, but with an active listener
            var completed = new List<Activity>();
            using var listener = CreateListener(completed);
            var id = Guid.NewGuid();
            var handler = new ContextCapturingHandler();
            var registry = new QueryHandlerRegistry();
            registry.Register<SyncTestQuery, SyncTestQuery.Result, ContextCapturingHandler>();
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => throw new NotImplementedException());
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var handlerConfig = new HandlerConfiguration(
                registry, new SimpleHandlerFactory(_ => handler), decoratorRegistry, decoratorFactory);
            var processor = new QueryProcessor(handlerConfig, new InMemoryQueryContextFactory());
            // No tracer — existing behaviour unchanged

            // Act
            var result = processor.Execute(new SyncTestQuery(id));

            // Assert — no span created even though a listener is registered
            result.ShouldNotBeNull();
            completed.Count.ShouldBe(0);
            handler.CapturedContext.Span.ShouldBeNull();
        }
    }
}
