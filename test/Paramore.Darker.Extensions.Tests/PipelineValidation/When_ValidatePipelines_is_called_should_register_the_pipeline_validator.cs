using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.PipelineValidation;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class PipelineValidationOptInTests
    {
        [Fact]
        public void When_ValidatePipelines_is_called_should_register_the_pipeline_validator()
        {
            // Arrange — the opt-in call on the builder AddDarker returns
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker().ValidatePipelines();

            // Act
            using var provider = services.BuildServiceProvider();
            var pipelineValidators = provider.GetServices<IAmAPipelineValidator>().OfType<PipelineValidator>();

            // Assert — exactly one pipeline validator (other validator kinds may join it later)
            pipelineValidators.Count().ShouldBe(1);
        }

        [Fact]
        public void When_ValidatePipelines_is_not_called_should_register_no_validator_and_no_hosted_service()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

            // Act — AddDarker alone, no opt-in
            services.AddDarker();

            // Assert — the application is unaffected
            services.Count(d => d.ServiceType == typeof(IAmAPipelineValidator)).ShouldBe(0);
            services.Count(d => d.ServiceType == typeof(IHostedService)).ShouldBe(0);
        }
    }
}
