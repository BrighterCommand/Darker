using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class HandlerResolvabilityTests
    {
        [Fact]
        public void When_a_handler_descriptor_is_removed_should_report_handler_resolvable()
        {
            // Arrange — a registered handler whose container descriptor is then removed
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker()
                .AddHandlers(r => r.Register<RemovedHandlerQuery, RemovedHandlerResult, RemovedHandlerQueryHandler>())
                .ValidatePipelines();
            services.RemoveAll<RemovedHandlerQueryHandler>();
            using var provider = services.BuildServiceProvider();

            // Act — validate through the real container
            var result = provider.ValidateAll();

            // Assert — an Error names the handler
            result.Errors.ShouldContain(e =>
                e.RuleId == new ValidationRuleId("HandlerResolvable")
                && e.Severity == ValidationSeverity.Error
                && e.Message.Contains(nameof(RemovedHandlerQueryHandler)));
        }
    }
}
