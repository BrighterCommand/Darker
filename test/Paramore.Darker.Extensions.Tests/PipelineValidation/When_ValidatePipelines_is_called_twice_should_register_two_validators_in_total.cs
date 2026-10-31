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
    public class ValidatePipelinesTwiceRegistersTwoValidatorsTests
    {
        [Fact]
        public void When_ValidatePipelines_is_called_twice_should_register_two_validators_in_total()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

            // Act — the opt-in is called twice on the same builder
            services.AddDarker().ValidatePipelines().ValidatePipelines();

            // Assert — the pipeline validator and the lifetime validator, once each, not three
            services.Count(d => d.ServiceType == typeof(IAmAPipelineValidator)).ShouldBe(2);
        }
    }
}
