using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Core.Tests.Logging;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_pipeline_scope_dispose_throws_should_log_error_and_return_result
    {
        private readonly LoggerCaptureFixture _logs;

        public When_pipeline_scope_dispose_throws_should_log_error_and_return_result(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public void When_the_scope_throws_on_dispose_on_execute_should_return_the_result_and_log_at_error()
        {
            // Arrange
            var fault = new InvalidOperationException("pipeline scope failed to dispose");
            var registry = new QueryHandlerRegistry();
            registry.Register<ThrowingScopeQuery, ThrowingScopeResult, ThrowingScopeQueryHandler>();
            var processor = BuildProcessor(new ThrowingScope(fault), registry: registry);

            // Act
            var result = processor.Execute(new ThrowingScopeQuery());

            // Assert
            result.Value.ShouldBe(ThrowingScopeQueryHandler.RESULT);
            AssertFaultLoggedOnceAtError(fault);
        }

        [Fact]
        public async Task When_the_scope_faults_on_dispose_async_on_execute_async_should_return_the_result_and_log_at_error()
        {
            // Arrange
            var fault = new InvalidOperationException("pipeline scope failed to dispose asynchronously");
            var registryAsync = new QueryHandlerRegistryAsync();
            registryAsync.Register<ThrowingScopeAsyncQuery, ThrowingScopeAsyncResult, ThrowingScopeAsyncQueryHandler>();
            var processor = BuildProcessor(new ThrowingScope(fault), registryAsync: registryAsync);

            // Act
            var result = await processor.ExecuteAsync(new ThrowingScopeAsyncQuery());

            // Assert
            result.Value.ShouldBe(ThrowingScopeAsyncQueryHandler.RESULT);
            AssertFaultLoggedOnceAtError(fault);
        }

        [Fact]
        public async Task When_the_scope_faults_on_dispose_async_on_execute_stream_should_yield_the_items_and_log_at_error()
        {
            // Arrange
            var fault = new InvalidOperationException("pipeline scope failed to dispose asynchronously on a stream");
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<ThrowingScopeStreamQuery, ThrowingScopeStreamItem, ThrowingScopeStreamQueryHandler>();
            var processor = BuildProcessor(new ThrowingScope(fault), streamRegistry: streamRegistry);
            var items = new List<ThrowingScopeStreamItem>();

            // Act
            await foreach (var item in processor.ExecuteStream(new ThrowingScopeStreamQuery()))
                items.Add(item);

            // Assert
            items.Single().Value.ShouldBe(ThrowingScopeStreamQueryHandler.ITEM);
            AssertFaultLoggedOnceAtError(fault);
        }

        private void AssertFaultLoggedOnceAtError(Exception fault)
        {
            // The fault is logged once at Error, carrying the exception and naming the scope's type
            var errors = _logs.CapturedLogs
                .Where(e => e.LogLevel == LogLevel.Error && ReferenceEquals(e.Exception, fault))
                .ToList();
            errors.Count.ShouldBe(1);
            errors[0].RenderedMessage.ShouldContain(nameof(ThrowingScope));
        }

        private static QueryProcessor BuildProcessor(
            IAmAScope scope,
            QueryHandlerRegistry registry = null,
            QueryHandlerRegistryAsync registryAsync = null,
            StreamQueryHandlerRegistry streamRegistry = null)
        {
            var handlerFactory = new FixedScopeHandlerFactory(scope);
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);
            var config = new HandlerConfiguration(
                registry ?? new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                registryAsync ?? new QueryHandlerRegistryAsync(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                streamRegistry ?? new StreamQueryHandlerRegistry());

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }
    }
}
