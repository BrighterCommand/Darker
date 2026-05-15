using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Testing.Ports;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests.Integrations
{
    public class AsyncConsumerDIResolutionTests
    {
        [Fact]
        public async Task When_async_consumer_resolves_IQueryProcessorAsync_from_DI_should_execute_query()
        {
            // Arrange
            var services = new ServiceCollection();
            var loggerFactory = LoggerFactory.Create(builder => { });
            services.AddSingleton<ILoggerFactory>(loggerFactory);
            services.AddDarker().AddHandlersFromAssemblies(typeof(TestQueryHandler).Assembly);

            var provider = services.BuildServiceProvider();
            var id = Guid.NewGuid();

            // Act
            var queryProcessor = provider.GetService<IQueryProcessorAsync>();
            queryProcessor.ShouldNotBeNull();
            var result = await queryProcessor.ExecuteAsync(new TestQueryA(id));

            // Assert
            result.ShouldBe(id);
        }
    }
}
