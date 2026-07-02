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
    [Collection("DarkerActivitySource")]
    public class QueryProcessorAsyncTracingTests
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
            QueryHandlerRegistryAsync asyncRegistry,
            SimpleHandlerFactory factory)
        {
            var syncRegistry = new QueryHandlerRegistry();
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => throw new NotImplementedException());
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var handlerConfig = new HandlerConfiguration(
                syncRegistry, factory, decoratorRegistry, decoratorFactory,
                asyncRegistry, factory, decoratorRegistry, decoratorFactory);
            return new QueryProcessor(
                handlerConfig,
                new InMemoryQueryContextFactory(),
                tracer: tracer,
                instrumentationOptions: InstrumentationOptions.QueryInformation);
        }

        [Fact]
        public async Task When_executing_async_query_with_tracer_should_create_and_end_query_span()
        {
            //Arrange
            var completed = new List<Activity>();
            using var listener = CreateListener(completed);
            using var tracer = new DarkerTracer();
            var id = Guid.NewGuid();
            var handler = new AsyncContextCapturingHandler();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncContextCapturingHandler>();
            var processor = CreateProcessorWithTracer(tracer, asyncRegistry, new SimpleHandlerFactory(_ => handler));

            //Act
            var result = await processor.ExecuteAsync(new AsyncTestQuery(id));

            //Assert
            result.Value.ShouldBe(id);
            completed.Count.ShouldBe(1);
            var span = completed[0];
            span.DisplayName.ShouldBe("AsyncTestQuery query");
            span.Status.ShouldBe(ActivityStatusCode.Ok);
            handler.CapturedContext.Span.ShouldNotBeNull();
            handler.CapturedContext.Span.ShouldBeSameAs(span);
        }

        [Fact]
        public async Task When_throwing_async_handler_should_set_error_status_and_propagate_unwrapped_exception()
        {
            //Arrange
            var completed = new List<Activity>();
            using var listener = CreateListener(completed);
            using var tracer = new DarkerTracer();
            var id = Guid.NewGuid();
            var handler = new RecordingQueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>(
                _ => throw new InvalidOperationException("boom"));
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, RecordingQueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>>();
            var processor = CreateProcessorWithTracer(tracer, asyncRegistry, new SimpleHandlerFactory(_ => handler));

            //Act — exception propagates unwrapped (not TargetInvocationException)
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => processor.ExecuteAsync(new AsyncTestQuery(id)));

            //Assert — exception propagates with original message
            ex.Message.ShouldBe("boom");

            //Assert — span ended with Error status and recorded exception event
            completed.Count.ShouldBe(1);
            var span = completed[0];
            span.Status.ShouldBe(ActivityStatusCode.Error);
            span.Events.Any(e => e.Name == "exception").ShouldBeTrue();
        }

        [Fact]
        public async Task When_executing_async_query_should_restore_Activity_Current_after_execution()
        {
            //Arrange
            var completed = new List<Activity>();
            using var listener = CreateListener(completed);
            using var tracer = new DarkerTracer();
            var id = Guid.NewGuid();
            var handler = new AsyncContextCapturingHandler();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, AsyncContextCapturingHandler>();
            var processor = CreateProcessorWithTracer(tracer, asyncRegistry, new SimpleHandlerFactory(_ => handler));
            var priorCurrent = Activity.Current;

            //Act
            await processor.ExecuteAsync(new AsyncTestQuery(id));

            //Assert — Activity.Current is restored to its pre-call value
            Activity.Current.ShouldBe(priorCurrent);
        }
    }
}
