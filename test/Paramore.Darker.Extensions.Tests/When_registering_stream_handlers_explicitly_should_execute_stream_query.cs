using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Core.Tests.Exported;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class AddStreamHandlersExplicitRegistrationTests
    {
        [Fact]
        public async Task When_registering_stream_handler_explicitly_via_builder_should_execute_stream_query_and_yield_items()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddStreamHandlers(r => r.Register<ExportedStreamQuery, string, ExportedStreamQueryHandler>());

            var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
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
