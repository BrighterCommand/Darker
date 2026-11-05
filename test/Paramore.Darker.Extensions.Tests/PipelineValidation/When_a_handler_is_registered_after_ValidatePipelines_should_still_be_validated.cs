using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class LateRegistrationValidationTests
    {
        [Fact]
        public void When_a_handler_is_registered_after_ValidatePipelines_should_still_be_validated()
        {
            // Arrange - opt in first, then register a duplicate-step handler on the same builder
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            var darker = services.AddDarker();
            darker.ValidatePipelines();
            darker.AddHandlers(r => r.Register<DuplicateStepQuery, DuplicateStepResult, DuplicateStepQueryHandler>());
            using var provider = services.BuildServiceProvider();

            // Act - validate through the real container
            var result = provider.ValidateAll();

            // Assert - the late handler's DuplicateStep Warning is reported
            result.Warnings.ShouldContain(w =>
                w.RuleId == new ValidationRuleId("DuplicateStep") && w.Message.Contains(nameof(DuplicateStepQueryHandler)));
        }
    }
}
