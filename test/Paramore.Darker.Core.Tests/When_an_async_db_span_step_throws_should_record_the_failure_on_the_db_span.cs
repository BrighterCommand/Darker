using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Observability;
using Paramore.Darker.Observability.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    [Collection("DarkerActivitySource")]
    public class QueryDbSpanFailureAsyncTests
    {
        [Fact]
        public async Task When_an_async_db_span_step_throws_should_record_the_failure_on_the_db_span()
        {
            // Arrange — the work inside [QueryDbSpanAttributeAsync] faults
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
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AsyncTestQuery, AsyncTestQuery.Result, ThrowingAsyncHandlerWithDbSpanAttribute>();
            var handlerFactory = new SimpleHandlerFactory(_ => new ThrowingAsyncHandlerWithDbSpanAttribute());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                new QueryDbSpanDecoratorAsync<IQuery<AsyncTestQuery.Result>, AsyncTestQuery.Result>());
            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, decoratorRegistry, decoratorFactory,
                asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory);
            var processor = new QueryProcessor(
                config,
                new InMemoryQueryContextFactory(),
                tracer: tracer,
                instrumentationOptions: InstrumentationOptions.QueryInformation | InstrumentationOptions.DatabaseInformation);

            // Act
            await Should.ThrowAsync<InvalidOperationException>(() =>
                processor.ExecuteAsync(new AsyncTestQuery(Guid.NewGuid()), cancellationToken: TestContext.Current.CancellationToken));

            // Assert — the DB span reports its own failure
            var dbSpan = completed.Single(a => a.Kind == ActivityKind.Client);
            dbSpan.Status.ShouldBe(ActivityStatusCode.Error);
            dbSpan.Events.Count(e => e.Name == "exception").ShouldBe(1);
            dbSpan.GetTagItem(DarkerSemanticConventions.ErrorType).ShouldBe(nameof(InvalidOperationException));

            // Assert — the query span still records the failure once, as its owner
            var querySpan = completed.Single(a => a.Kind == ActivityKind.Internal);
            querySpan.Status.ShouldBe(ActivityStatusCode.Error);
            querySpan.Events.Count(e => e.Name == "exception").ShouldBe(1);
        }
    }
}
