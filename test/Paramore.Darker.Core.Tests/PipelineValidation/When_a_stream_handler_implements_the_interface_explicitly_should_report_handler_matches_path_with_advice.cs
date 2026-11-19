using System.Linq;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class StreamHandlerExplicitInterfaceRuleTests
    {
        private static readonly ValidationRuleId HandlerMatchesPath = new("HandlerMatchesPath");

        [Fact]
        public void When_a_stream_handler_implements_the_interface_explicitly_should_report_handler_matches_path_with_advice()
        {
            // Arrange — a stream handler whose ExecuteAsync is an explicit interface member only
            var registry = new StreamQueryHandlerRegistry();
            registry.Register(typeof(StreamTestQuery), typeof(string), typeof(ExplicitInterfaceStreamHandler));
            var pipelineValidator = new PipelineValidatorBuilder().WithStreamRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one error naming the handler and advising a public implementation
            var finding = result.Errors.Single(e => e.RuleId == HandlerMatchesPath);
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain(nameof(ExplicitInterfaceStreamHandler));
            finding.Message.ShouldContain("'IStreamQueryHandler<,>' only explicitly");
            finding.Message.ShouldContain("public");
        }
    }
}
