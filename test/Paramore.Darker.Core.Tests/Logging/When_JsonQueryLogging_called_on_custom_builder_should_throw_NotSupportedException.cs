using System;
using System.Text.Json;
using Paramore.Darker.Builder;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Logging;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Logging
{
    public class When_JsonQueryLogging_called_on_custom_builder_should_throw_NotSupportedException
    {
        [Fact]
        public void Custom_IBuildTheQueryProcessor_is_not_supported_by_the_builder_surface()
        {
            // Arrange — a custom builder that is not the in-box QueryProcessorBuilder
            var customBuilder = new CustomQueryProcessorBuilder();

            // Act
            var exception = Should.Throw<NotSupportedException>(
                () => customBuilder.JsonQueryLogging(o => o.WriteIndented = true));

            // Assert — the message references QueryProcessorBuilder so the constraint is discoverable
            // (documented limitation, ADR Decision step 5; matches the Policies precedent).
            exception.Message.ShouldContain(nameof(QueryProcessorBuilder));
        }
    }
}
