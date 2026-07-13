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
    public class AddDarkerStreamHandlerRegistrationTests
    {
        [Fact]
        public async Task When_AddHandlersFromAssemblies_scans_assembly_should_register_stream_handlers_so_ExecuteStream_yields_items()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker().AddHandlersFromAssemblies(typeof(ExportedStreamQueryHandler).Assembly);

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
