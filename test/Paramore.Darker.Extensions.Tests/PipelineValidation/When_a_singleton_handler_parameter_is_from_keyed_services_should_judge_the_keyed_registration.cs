using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class KeyedCaptiveDependencyTests
    {
        [Fact]
        public void When_a_keyed_singleton_sits_beside_an_unkeyed_scoped_should_yield_nothing()
        {
            // Arrange — the parameter is bound to the keyed Singleton "k"; an unkeyed Scoped of the same type is ignored
            var services = new ServiceCollection();
            services.AddKeyedSingleton<IKeyedDb, KeyedDb>("k");
            services.AddScoped<IKeyedDb, KeyedDb>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, KeyedCaptiveHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert
            result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ShouldBeEmpty();
        }

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
