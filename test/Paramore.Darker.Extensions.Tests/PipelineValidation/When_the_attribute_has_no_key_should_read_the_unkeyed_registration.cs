using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class KeylessKeyedAttributeTests
    {
        [Fact]
        public void When_the_attribute_has_no_key_should_read_the_unkeyed_registration()
        {
            // Arrange — a parameterless [FromKeyedServices] reads the unkeyed Scoped registration
            var services = new ServiceCollection();
            services.AddScoped<IKeyedDb, KeyedDb>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, KeylessKeyedAttributeHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert
            var finding = result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ShouldHaveSingleItem();
            finding.Source.ShouldBe("Handler 'KeylessKeyedAttributeHandler'");
        }
    }
}
