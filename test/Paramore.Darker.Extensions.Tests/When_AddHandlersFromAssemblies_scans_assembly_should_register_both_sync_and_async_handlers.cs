using System;
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
    public class DependencyInjectionTests
    {
        [Fact]
        public void When_AddHandlersFromAssemblies_scans_assembly_should_register_sync_handlers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker().AddHandlersFromAssemblies(typeof(TestQueryHandler).Assembly);

            var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var id = Guid.NewGuid();

            // Act
            var result = queryProcessor.Execute(new TestQueryA(id));

            // Assert
            result.ShouldBe(id);
        }

        [Fact]
        public async Task When_AddHandlersFromAssemblies_scans_assembly_should_register_async_handlers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker().AddHandlersFromAssemblies(typeof(TestQueryHandlerAsync).Assembly);

            var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var id = Guid.NewGuid();

            // Act
            var result = await queryProcessor.ExecuteAsync(new TestQueryA(id));

            // Assert
            result.ShouldBe(id);
        }

        [Fact]
        public async Task When_AddHandlersFromAssemblies_scans_assembly_should_wire_both_sync_and_async_independently()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker().AddHandlersFromAssemblies(typeof(TestQueryHandler).Assembly);

            var provider = services.BuildServiceProvider();
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();
            var syncId = Guid.NewGuid();
            var asyncId = Guid.NewGuid();

            // Act
            var syncResult = queryProcessor.Execute(new TestQueryA(syncId));
            var asyncResult = await queryProcessor.ExecuteAsync(new TestQueryA(asyncId));

            // Assert
            syncResult.ShouldBe(syncId);
            asyncResult.ShouldBe(asyncId);
        }
    }
}
