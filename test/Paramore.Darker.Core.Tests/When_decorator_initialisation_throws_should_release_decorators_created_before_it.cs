using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_decorator_initialisation_throws_should_release_decorators_created_before_it
    {
        private readonly DecoratorInitFaultException _fault = new DecoratorInitFaultException("decorator failed to initialise");
        private readonly List<IQueryHandlerDecorator> _created = new List<IQueryHandlerDecorator>();
        private IQueryHandler _handler;
        private RecordingHandlerFactory _handlerFactory;
        private RecordingDecoratorFactory _decoratorFactory;

        [Fact]
        public void When_a_decorator_throws_during_initialisation_on_execute_should_release_the_handler_and_every_created_decorator()
        {
            // Arrange — the second decorator created throws from InitializeFromAttributeParams
            var registry = new QueryHandlerRegistry();
            registry.Register<PartialBuildQuery, string, PartialBuildHandler>();
            var processor = BuildProcessor(registry, new QueryHandlerRegistryAsync(), new StreamQueryHandlerRegistry(),
                _ => _handler = new PartialBuildHandler(), throwOnCreate: false);

            // Act
            var thrown = Should.Throw<DecoratorInitFaultException>(() => processor.Execute(new PartialBuildQuery()));

            // Assert
            thrown.ShouldBeSameAs(_fault);
            AssertHandlerAndAllCreatedDecoratorsReleased(expectedDecorators: 2);
        }

        [Fact]
        public async Task When_a_decorator_throws_during_initialisation_on_execute_async_should_release_the_handler_and_every_created_decorator()
        {
            // Arrange
            var registryAsync = new QueryHandlerRegistryAsync();
            registryAsync.Register<PartialBuildAsyncQuery, string, PartialBuildAsyncHandler>();
            var processor = BuildProcessor(new QueryHandlerRegistry(), registryAsync, new StreamQueryHandlerRegistry(),
                _ => _handler = new PartialBuildAsyncHandler(), throwOnCreate: false);

            // Act
            var thrown = await Should.ThrowAsync<DecoratorInitFaultException>(
                () => processor.ExecuteAsync(new PartialBuildAsyncQuery()));

            // Assert
            thrown.ShouldBeSameAs(_fault);
            AssertHandlerAndAllCreatedDecoratorsReleased(expectedDecorators: 2);
        }

        [Fact]
        public async Task When_a_decorator_throws_during_initialisation_on_execute_stream_should_release_the_handler_and_every_created_decorator()
        {
            // Arrange
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<PartialBuildStreamQuery, string, PartialBuildStreamHandler>();
            var processor = BuildProcessor(new QueryHandlerRegistry(), new QueryHandlerRegistryAsync(), streamRegistry,
                _ => _handler = new PartialBuildStreamHandler(), throwOnCreate: false);

            // Act
            var thrown = await Should.ThrowAsync<DecoratorInitFaultException>(async () =>
            {
                await foreach (var _ in processor.ExecuteStream(new PartialBuildStreamQuery()))
                {
                }
            });

            // Assert
            thrown.ShouldBeSameAs(_fault);
            AssertHandlerAndAllCreatedDecoratorsReleased(expectedDecorators: 2);
        }

        [Fact]
        public void When_a_decorator_cannot_be_created_on_execute_should_release_the_handler_and_the_decorator_created_before_it()
        {
            // Arrange — the decorator factory itself throws when asked for the second decorator
            var registry = new QueryHandlerRegistry();
            registry.Register<PartialBuildQuery, string, PartialBuildHandler>();
            var processor = BuildProcessor(registry, new QueryHandlerRegistryAsync(), new StreamQueryHandlerRegistry(),
                _ => _handler = new PartialBuildHandler(), throwOnCreate: true);

            // Act
            var thrown = Should.Throw<DecoratorInitFaultException>(() => processor.Execute(new PartialBuildQuery()));

            // Assert — only the first decorator was ever created, and it is released along with the handler
            thrown.ShouldBeSameAs(_fault);
            AssertHandlerAndAllCreatedDecoratorsReleased(expectedDecorators: 1);
        }

        private QueryProcessor BuildProcessor(
            QueryHandlerRegistry registry,
            QueryHandlerRegistryAsync registryAsync,
            StreamQueryHandlerRegistry streamRegistry,
            Func<Type, IQueryHandler> createHandler,
            bool throwOnCreate)
        {
            _handlerFactory = new RecordingHandlerFactory(createHandler);
            _decoratorFactory = new RecordingDecoratorFactory(decoratorType =>
            {
                var isThrowingDecorator = decoratorType.GetGenericTypeDefinition().Name.StartsWith("ThrowingInit");
                if (isThrowingDecorator && throwOnCreate)
                    throw _fault;

                var decorator = (IQueryHandlerDecorator)(isThrowingDecorator
                    ? Activator.CreateInstance(decoratorType, _fault)
                    : Activator.CreateInstance(decoratorType));
                _created.Add(decorator);
                return decorator;
            });

            var decoratorRegistry = new InMemoryDecoratorRegistry();
            var config = new HandlerConfiguration(
                registry, _handlerFactory, decoratorRegistry, _decoratorFactory,
                registryAsync, _handlerFactory, decoratorRegistry, _decoratorFactory,
                streamRegistry);

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }

        private void AssertHandlerAndAllCreatedDecoratorsReleased(int expectedDecorators)
        {
            _handlerFactory.ReleaseCount(_handler).ShouldBe(1);

            _created.Count.ShouldBe(expectedDecorators);
            _decoratorFactory.Released.Count.ShouldBe(expectedDecorators);
            _created.ShouldAllBe(decorator => _decoratorFactory.ReleaseCount(decorator) == 1);
        }
    }
}
