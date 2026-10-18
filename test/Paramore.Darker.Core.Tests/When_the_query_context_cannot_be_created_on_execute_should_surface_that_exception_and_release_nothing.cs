using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class NoComponentCreatedReleaseTests
    {
        // The registries are empty and the factories hand out nothing, so no handler or decorator is ever created.
        private static (QueryProcessor Processor, RecordingHandlerFactory Sync, RecordingHandlerFactory Async) Build(
            IQueryContextFactory queryContextFactory)
        {
            var syncFactory = new RecordingHandlerFactory(_ => throw new InvalidOperationException("no handler should be created"));
            var asyncFactory = new RecordingHandlerFactory(_ => throw new InvalidOperationException("no handler should be created"));
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ => null);

            var config = new HandlerConfiguration(
                new QueryHandlerRegistry(), syncFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new QueryHandlerRegistryAsync(), asyncFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                new StreamQueryHandlerRegistry());
            return (new QueryProcessor(config, queryContextFactory), syncFactory, asyncFactory);
        }

        private static void ShouldNotHaveReleasedAnything(RecordingHandlerFactory sync, RecordingHandlerFactory async)
        {
            sync.Released.ShouldBeEmpty();
            async.Released.ShouldBeEmpty();
        }

        [Fact]
        public void When_the_query_context_cannot_be_created_on_execute_should_surface_that_exception_and_release_nothing()
        {
            // Arrange
            var fault = new InvalidOperationException("query context could not be created");
            var (processor, sync, async) = Build(new ThrowingQueryContextFactory(fault));

            // Act
            var thrown = Should.Throw<InvalidOperationException>(() => processor.Execute(new LifetimeIsolationQuery()));

            // Assert — the caller sees the context factory's exception, and nothing was released
            thrown.ShouldBeSameAs(fault);
            ShouldNotHaveReleasedAnything(sync, async);
        }

        [Fact]
        public async Task When_the_query_context_cannot_be_created_on_execute_async_should_surface_that_exception_and_release_nothing()
        {
            // Arrange
            var fault = new InvalidOperationException("query context could not be created");
            var (processor, sync, async) = Build(new ThrowingQueryContextFactory(fault));

            // Act
            var thrown = await Should.ThrowAsync<InvalidOperationException>(
                async () => await processor.ExecuteAsync(new AsyncLifetimeFaultQuery()));

            // Assert — the caller sees the context factory's exception, and nothing was released
            thrown.ShouldBeSameAs(fault);
            ShouldNotHaveReleasedAnything(sync, async);
        }

        [Fact]
        public async Task When_the_query_context_cannot_be_created_on_execute_stream_should_surface_that_exception_at_first_move_next_and_release_nothing()
        {
            // Arrange
            var fault = new InvalidOperationException("query context could not be created");
            var (processor, sync, async) = Build(new ThrowingQueryContextFactory(fault));

            // Act
            var thrown = await Should.ThrowAsync<InvalidOperationException>(async () =>
            {
                await foreach (var _ in processor.ExecuteStream(new StreamLifetimeFaultQuery()))
                {
                }
            });

            // Assert — the caller sees the context factory's exception, and nothing was released
            thrown.ShouldBeSameAs(fault);
            ShouldNotHaveReleasedAnything(sync, async);
        }

        [Fact]
        public void When_no_handler_is_registered_on_execute_should_surface_the_configuration_exception_and_release_nothing()
        {
            // Arrange
            var (processor, sync, async) = Build(new InMemoryQueryContextFactory());

            // Act / Assert — the lookup failure reaches the caller
            Should.Throw<ConfigurationException>(() => processor.Execute(new LifetimeIsolationQuery()));
            ShouldNotHaveReleasedAnything(sync, async);
        }

        [Fact]
        public async Task When_no_handler_is_registered_on_execute_async_should_surface_the_configuration_exception_and_release_nothing()
        {
            // Arrange
            var (processor, sync, async) = Build(new InMemoryQueryContextFactory());

            // Act / Assert — the lookup failure reaches the caller
            await Should.ThrowAsync<ConfigurationException>(
                async () => await processor.ExecuteAsync(new AsyncLifetimeFaultQuery()));
            ShouldNotHaveReleasedAnything(sync, async);
        }

        [Fact]
        public async Task When_no_handler_is_registered_on_execute_stream_should_surface_the_configuration_exception_and_release_nothing()
        {
            // Arrange
            var (processor, sync, async) = Build(new InMemoryQueryContextFactory());

            // Act / Assert — the lookup failure reaches the caller
            await Should.ThrowAsync<ConfigurationException>(async () =>
            {
                await foreach (var _ in processor.ExecuteStream(new StreamLifetimeFaultQuery()))
                {
                }
            });
            ShouldNotHaveReleasedAnything(sync, async);
        }
    }
}
