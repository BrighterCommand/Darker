using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests
{
    public class ScopedDependencyUnderSingletonProcessorLifetimeTests
    {
        [Fact]
        public void When_query_processor_is_singleton_and_dependency_is_scoped_should_resolve_from_per_query_scope_and_dispose()
        {
            // Arrange — default Singleton QueryProcessor, a Scoped dependency (an EF Core DbContext
            // stand-in), and scope validation ON so resolving Scoped from the root provider would throw
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: ServiceLifetime.Scoped,
                dependencyLifetime: ServiceLifetime.Scoped,
                validateScopes: true);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act — execute the query through the singleton processor
            var result = queryProcessor.Execute(new TrackedQuery());

            // Assert — the scoped dependency resolves (no root-provider error) and is disposed after the pipeline
            result.HandlerDependency.ShouldNotBeNull();
            result.HandlerDependency.IsDisposed.ShouldBeTrue();
        }

        [Fact]
        public async Task When_query_processor_is_singleton_and_dependency_is_scoped_should_resolve_from_per_query_scope_and_dispose_async()
        {
            // Arrange — default Singleton QueryProcessor, a Scoped dependency (an EF Core DbContext
            // stand-in), and scope validation ON so resolving Scoped from the root provider would throw
            var provider = TrackedDependencyScenario.Build(
                handlerLifetime: ServiceLifetime.Scoped,
                dependencyLifetime: ServiceLifetime.Scoped,
                validateScopes: true);
            var queryProcessor = provider.GetRequiredService<IQueryProcessor>();

            // Act — execute the query through the singleton processor
            var result = await queryProcessor.ExecuteAsync(new TrackedQuery());

            // Assert — the scoped dependency resolves (no root-provider error) and is disposed after the pipeline
            result.HandlerDependency.ShouldNotBeNull();
            result.HandlerDependency.IsDisposed.ShouldBeTrue();
        }
    }
}
