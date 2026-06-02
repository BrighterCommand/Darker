using System;
using Paramore.Darker.Logging;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Logging
{
    [Collection("QueryLoggingJsonOptions")]
    public class When_QueryLoggingJsonOptions_Options_setter_receives_null_should_throw_ArgumentNullException
    {
        [Fact]
        public void Assigning_null_throws_and_leaves_the_prior_instance_intact()
        {
            // Arrange — keep the prior non-null value so we can assert it survives the failed set
            var original = QueryLoggingJsonOptions.Options;
            try
            {
                // Act — assign null
                var exception = Should.Throw<ArgumentNullException>(
                    () => QueryLoggingJsonOptions.Options = null!);

                // Assert — fail-fast guard names the offending parameter ...
                exception.ParamName.ShouldBe("value");

                // ... and the prior instance is untouched (no partial-state corruption)
                QueryLoggingJsonOptions.Options.ShouldBeSameAs(original);
            }
            finally
            {
                QueryLoggingJsonOptions.Options = original;
            }
        }
    }
}
