using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class LateScopedDependencyTests
    {
        [Fact]
        public void When_a_scoped_dependency_is_added_after_ValidatePipelines_should_not_be_reported()
        {
            // Arrange — a Singleton HandlerLifetime handler taking ScopedDependency, registered after ValidatePipelines()
            var services = new ServiceCollection();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, CaptiveHandler>())
                .ValidatePipelines();
            services.AddScoped<ScopedDependency>();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — the snapshot predates the Scoped registration, so no CaptiveDependency is reported
            result.Errors.Concat(result.Warnings).Where(f => f.RuleId == new ValidationRuleId("CaptiveDependency")).ShouldBeEmpty();
        }
    }
}
