using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class PipelineValidationIdempotenceTests
    {
        [Fact]
        public void When_ValidatePipelines_is_called_twice_should_register_once()
        {
            // Arrange - ValidatePipelines is called twice on the same builder, with a Fallback-inside-Retry handler
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddHandlers(r => r.Register<OrderingViolationQuery, OrderingViolationResult, OrderingViolationQueryHandler>())
                .AddDefaultPolicies()
                .ValidatePipelines()
                .ValidatePipelines();
            using var provider = services.BuildServiceProvider();

            // Act - resolve the validators and validate through the real container
            var validators = provider.GetServices<IAmAPipelineValidator>().OfType<PipelineValidator>().ToList();
            var result = provider.ValidateAll();

            // Assert - one pipeline validator, and one ordering warning rather than a repeated one
            validators.Count.ShouldBe(1);
            result.Warnings.Count(w => w.RuleId == new ValidationRuleId("DecoratorOrdering")).ShouldBe(1);
        }
    }
}
