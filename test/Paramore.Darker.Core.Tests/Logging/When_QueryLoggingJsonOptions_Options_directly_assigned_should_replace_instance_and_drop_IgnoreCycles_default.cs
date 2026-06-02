using System.Text.Json;
using Paramore.Darker.Logging;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Logging
{
    [Collection("QueryLoggingJsonOptions")]
    public class When_QueryLoggingJsonOptions_Options_directly_assigned_should_replace_instance_and_drop_IgnoreCycles_default
    {
        [Fact]
        public void Direct_assignment_replaces_the_instance_and_does_not_re_apply_IgnoreCycles()
        {
            // Arrange — save the prior reference so the global is restored afterwards (C5)
            var original = QueryLoggingJsonOptions.Options;
            try
            {
                // Act — replace the instance entirely (the "you own all the defaults" path)
                var fresh = new JsonSerializerOptions();
                QueryLoggingJsonOptions.Options = fresh;

                // Assert — the setter swaps the reference; it does not merge or defensively copy ...
                QueryLoggingJsonOptions.Options.ShouldBeSameAs(fresh);

                // ... and it does NOT re-apply the FR3 default. Direct assignment is lossy by
                // contract (ADR Decision step 7): consumers must re-apply IgnoreCycles themselves.
                // This guards against a future "helpful" setter refactor that auto-applies it.
                QueryLoggingJsonOptions.Options.ReferenceHandler.ShouldBeNull();
            }
            finally
            {
                QueryLoggingJsonOptions.Options = original;
            }
        }
    }
}
