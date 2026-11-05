using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class CaptiveDependencyClosedGenericTests
    {
        [Fact]
        public void When_a_singleton_handler_takes_a_closed_generic_of_a_scoped_open_generic_should_report_captive_dependency()
        {
            // Arrange — a Singleton handler taking IRepository<Order>, with only the open generic IRepository<> registered Scoped
            var services = new ServiceCollection();
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Singleton)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, OrderRepositoryHandler>())
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act
            var result = provider.ValidateAll();

            // Assert — one Warning naming the parameter
            var captive = result.Warnings.Where(w => w.RuleId == new ValidationRuleId("CaptiveDependency")).ToList();
            var finding = captive.ShouldHaveSingleItem();
            finding.Severity.ShouldBe(ValidationSeverity.Warning);
            finding.Source.ShouldBe("Handler 'OrderRepositoryHandler'");
            finding.Message.ShouldContain("'repository'");
        }
    }
}
