using System;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    public class PipelineValidationForeignBuilderTests
    {
        [Fact]
        public void When_ValidatePipelines_is_called_on_a_foreign_builder_should_throw_invalid_operation()
        {
            // Arrange — a builder that AddDarker did not create
            var builder = new ForeignDarkerHandlerBuilder();

            // Act
            var exception = Should.Throw<InvalidOperationException>(() => builder.ValidatePipelines());

            // Assert — the message tells the caller where the builder must come from
            exception.Message.ShouldContain("AddDarker");
        }
    }
}
