using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class DuplicateScopeProviderFactoryTests
    {
        private static ValidationError Validate(ServiceCollection services)
        {
            services.AddDarker().ValidatePipelines();
            using var provider = services.BuildServiceProvider();
            var result = provider.ValidateAll();
            return result.Warnings.Where(w => w.RuleId == new ValidationRuleId("DuplicateScopeProvider")).ShouldHaveSingleItem();
        }

        [Fact]
        public void When_a_factory_scope_provider_sits_beside_a_type_registration_should_report_duplicate_scope_provider()
        {
            // Arrange — a factory registration first, then a type registration
            var services = new ServiceCollection();
            services.AddSingleton<IAmAScopeProvider>(_ => new TestScopeProvider());
            services.AddSingleton<IAmAScopeProvider, AsyncLocalScopeProvider>();

            // Act
            var finding = Validate(services);

            // Assert — the factory is rendered by position, the type by name
            finding.Severity.ShouldBe(ValidationSeverity.Warning);
            finding.Message.ShouldContain("a factory registration at position 1");
            finding.Message.ShouldContain("AsyncLocalScopeProvider");
        }

        [Fact]
        public void When_the_factory_scope_provider_is_the_one_in_use_should_render_it_by_position()
        {
            // Arrange — a type registration first, then a factory registration that wins
            var services = new ServiceCollection();
            services.AddSingleton<IAmAScopeProvider, AsyncLocalScopeProvider>();
            services.AddSingleton<IAmAScopeProvider>(_ => new TestScopeProvider());

            // Act
            var finding = Validate(services);

            // Assert
            finding.Message.ShouldContain("AsyncLocalScopeProvider");
            finding.Message.ShouldContain("a factory registration at position 2");
            finding.Message.ShouldContain("a factory registration at position 2 is used");
        }

        [Fact]
        public void When_two_factory_scope_providers_are_registered_should_report_duplicate_scope_provider()
        {
            // Arrange — two factories and nothing else
            var services = new ServiceCollection();
            services.AddSingleton<IAmAScopeProvider>(_ => new TestScopeProvider());
            services.AddSingleton<IAmAScopeProvider>(_ => new TestScopeProvider());

            // Act
            var finding = Validate(services);

            // Assert — each factory is counted as distinct
            finding.Severity.ShouldBe(ValidationSeverity.Warning);
            finding.Message.ShouldContain("a factory registration at position 1");
            finding.Message.ShouldContain("a factory registration at position 2");
        }
    }
}
