using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Builder;
using Paramore.Darker.Core.Tests.Exported;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class QueryProcessorBuilderStreamTests
    {
        [Fact]
        public async Task When_QueryProcessorBuilder_configured_with_stream_registry_should_execute_stream_query()
        {
            // Arrange
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<ExportedStreamQuery, string, ExportedStreamQueryHandler>();

            var handlerFactory = new SimpleHandlerFactory(type =>
            {
                if (type == typeof(ExportedStreamQueryHandler)) return new ExportedStreamQueryHandler();
                return null!;
            });

            var queryProcessor = QueryProcessorBuilder.With()
                .Handlers(new HandlerConfiguration(
                    new QueryHandlerRegistry(),
                    handlerFactory,
                    new InMemoryDecoratorRegistry(),
                    new SimpleHandlerDecoratorFactory(type => null!),
                    new QueryHandlerRegistryAsync(),
                    handlerFactory,
                    new InMemoryDecoratorRegistry(),
                    new SimpleHandlerDecoratorFactory(type => null!),
                    streamRegistry))
                .InMemoryQueryContextFactory()
                .Build();

            var results = new List<string>();

            // Act
            await foreach (var item in queryProcessor.ExecuteStream(new ExportedStreamQuery()))
            {
                results.Add(item);
            }

            // Assert
            results.ShouldHaveSingleItem();
            results[0].ShouldBe("exported-item");
        }
    }
}
