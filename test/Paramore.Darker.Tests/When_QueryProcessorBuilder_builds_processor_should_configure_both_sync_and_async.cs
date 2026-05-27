using System;
using System.Threading.Tasks;
using Paramore.Darker.Builder;
using Paramore.Darker.Testing.Ports;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests
{
    public class QueryProcessorBuilderTests
    {
        [Fact]
        public void When_builder_builds_processor_should_execute_sync_query()
        {
            // Arrange
            var syncRegistry = new QueryHandlerRegistry();
            syncRegistry.Register<TestQueryA, Guid, TestQueryHandler>();

            var asyncRegistry = new QueryHandlerRegistryAsync();

            var queryProcessor = QueryProcessorBuilder.With()
                .Handlers(new HandlerConfiguration(
                    syncRegistry,
                    new SimpleHandlerFactory(type => new TestQueryHandler()),
                    new InMemoryDecoratorRegistry(),
                    new SimpleHandlerDecoratorFactory(type => null!),
                    asyncRegistry,
                    new SimpleHandlerFactory(type => null!),
                    new InMemoryDecoratorRegistry(),
                    new SimpleHandlerDecoratorFactory(type => null!)))
                .InMemoryQueryContextFactory()
                .Build();

            var id = Guid.NewGuid();

            // Act
            var result = queryProcessor.Execute(new TestQueryA(id));

            // Assert
            result.ShouldBe(id);
        }

        [Fact]
        public async Task When_builder_builds_processor_should_execute_async_query()
        {
            // Arrange
            var syncRegistry = new QueryHandlerRegistry();

            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<TestQueryA, Guid, TestQueryHandlerAsync>();

            var queryProcessor = QueryProcessorBuilder.With()
                .Handlers(new HandlerConfiguration(
                    syncRegistry,
                    new SimpleHandlerFactory(type => null!),
                    new InMemoryDecoratorRegistry(),
                    new SimpleHandlerDecoratorFactory(type => null!),
                    asyncRegistry,
                    new SimpleHandlerFactory(type => new TestQueryHandlerAsync()),
                    new InMemoryDecoratorRegistry(),
                    new SimpleHandlerDecoratorFactory(type => null!)))
                .InMemoryQueryContextFactory()
                .Build();

            var id = Guid.NewGuid();

            // Act
            var result = await queryProcessor.ExecuteAsync(new TestQueryA(id));

            // Assert
            result.ShouldBe(id);
        }

        [Fact]
        public void When_builder_registers_default_decorators_should_register_both_sync_and_async_fallback()
        {
            // Arrange
            var syncRegistry = new QueryHandlerRegistry();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            var syncDecoratorRegistry = new InMemoryDecoratorRegistry();
            var asyncDecoratorRegistry = new InMemoryDecoratorRegistry();

            var queryProcessor = QueryProcessorBuilder.With()
                .Handlers(new HandlerConfiguration(
                    syncRegistry,
                    new SimpleHandlerFactory(type => null!),
                    syncDecoratorRegistry,
                    new SimpleHandlerDecoratorFactory(type => null!),
                    asyncRegistry,
                    new SimpleHandlerFactory(type => null!),
                    asyncDecoratorRegistry,
                    new SimpleHandlerDecoratorFactory(type => null!)))
                .InMemoryQueryContextFactory()
                .Build();

            // Assert
            syncDecoratorRegistry.RegisteredTypes.ShouldContain(typeof(Paramore.Darker.Policies.Handlers.FallbackPolicyDecorator<,>));
            asyncDecoratorRegistry.RegisteredTypes.ShouldContain(typeof(Paramore.Darker.Policies.Handlers.FallbackPolicyDecoratorAsync<,>));
        }
    }
}
