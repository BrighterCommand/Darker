using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class KeyedScopedRegistrationTests
    {
        [Fact]
        public void When_the_keyed_registration_is_scoped_should_yield_one_warning()
        {
            // Arrange — the parameter is bound to the keyed Scoped "k"
            var services = new ServiceCollection();
            services.AddKeyedScoped<IKeyedDb, KeyedDb>("k");
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, KeyedCaptiveHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert
            var finding = result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ShouldHaveSingleItem();
            finding.Source.ShouldBe("Handler 'KeyedCaptiveHandler'");
        }
    }
}
