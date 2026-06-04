using System.Text.Json.Serialization;
using Paramore.Darker.Logging;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Logging
{
    [Collection("QueryLoggingJsonOptions")]
    public class When_QueryLoggingJsonOptions_accessed_without_configuration_should_expose_default_options_with_ReferenceHandler_IgnoreCycles
    {
        [Fact]
        public void Default_options_are_non_null_ignore_cycles_and_are_not_locked()
        {
            // Arrange — save the prior state so this test does not leak its mutation (C5)
            var original = QueryLoggingJsonOptions.Options;
            var originalMaxDepth = original.MaxDepth;
            try
            {
                // Act / Assert — the class-init default instance exists ...
                QueryLoggingJsonOptions.Options.ShouldNotBeNull();

                // ... carries the FR3 cycle handler ...
                QueryLoggingJsonOptions.Options.ReferenceHandler.ShouldBe(ReferenceHandler.IgnoreCycles);

                // ... and class-init did NOT lock the options (FR14): an in-place property
                // mutation on the existing instance is still accepted (no InvalidOperationException).
                Should.NotThrow(() => QueryLoggingJsonOptions.Options.MaxDepth = 32);
            }
            finally
            {
                QueryLoggingJsonOptions.Options.MaxDepth = originalMaxDepth;
                QueryLoggingJsonOptions.Options = original;
            }
        }
    }
}
