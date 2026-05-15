using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Testing.Ports;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests.Integrations
{
    public class SameInstanceResolutionTests
    {
        [Fact]
        public void When_both_interfaces_resolved_in_same_scope_should_be_same_instance()
        {
            // Arrange
            var services = new ServiceCollection();
            var loggerFactory = LoggerFactory.Create(builder => { });
            services.AddSingleton<ILoggerFactory>(loggerFactory);
            services.AddDarker(options => options.QueryProcessorLifetime = ServiceLifetime.Scoped)
                .AddHandlersFromAssemblies(typeof(TestQueryHandler).Assembly);

            var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            // Act
            var syncProcessor = scope.ServiceProvider.GetRequiredService<IQueryProcessor>();
            var asyncProcessor = scope.ServiceProvider.GetRequiredService<IQueryProcessorAsync>();

            // Assert
            syncProcessor.ShouldBeSameAs(asyncProcessor);
        }
    }
}
