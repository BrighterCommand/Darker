using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class CaptiveDependencyInterfaceAliasTests
    {
        [Fact]
        public void When_a_handler_has_an_interface_alias_registration_should_report_nothing()
        {
            // Arrange — a Transient HandlerLifetime, and the handler also aliased Singleton under its interface
            var services = new ServiceCollection();
            services.AddScoped<ScopedDependency>();
            services.AddSingleton<IQueryHandlerAsync<CaptiveQuery, CaptiveResult>, CaptiveHandlerAsync>();
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Transient)
                .AddHandlers(r => r.Register(typeof(CaptiveQuery), typeof(CaptiveResult), typeof(CaptiveHandlerAsync)))
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert — the alias does not make the handler Singleton
            result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ShouldBeEmpty();
        }
    }
}
