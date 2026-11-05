using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.PipelineValidation;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class ValidatePipelinesRegistersLifetimeValidatorTests
    {
        [Fact]
        public void When_ValidatePipelines_is_called_should_register_the_lifetime_validator_after_the_pipeline_validator()
        {
            // Arrange — the opt-in call on the builder AddDarker returns
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            services.AddDarker().ValidatePipelines();

            // Act
            using var provider = services.BuildServiceProvider();
            var validators = provider.GetServices<IAmAPipelineValidator>().ToList();

            // Assert — two validators, the pipeline validator first, then the lifetime validator
            services.Count(d => d.ServiceType == typeof(IAmAPipelineValidator)).ShouldBe(2);
            validators.Count.ShouldBe(2);
            validators[0].ShouldBeOfType<PipelineValidator>();
            validators[1].ShouldNotBeOfType<PipelineValidator>();
        }
    }
}
