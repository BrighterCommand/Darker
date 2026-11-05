using System;
using Paramore.Darker.PipelineValidation;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class PipelineValidatorConstructionTests
    {
        [Fact]
        public void When_constructing_a_validator_with_null_ordering_rules_should_throw()
        {
            // Arrange / Act — the constructor is called directly because the guard is the constructor's own
            var exception = Should.Throw<ArgumentNullException>(() => new PipelineValidator(null, null, null, null!));

            // Assert
            exception.ParamName.ShouldBe("orderingRules");
        }
    }
}
