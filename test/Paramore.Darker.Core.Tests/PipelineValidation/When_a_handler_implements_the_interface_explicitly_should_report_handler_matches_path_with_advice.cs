using System.Linq;
using Paramore.Darker.Core.Tests.Exported;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class HandlerExplicitInterfaceRuleTests
    {
        private static readonly ValidationRuleId HandlerMatchesPath = new("HandlerMatchesPath");

        [Fact]
        public void When_a_handler_implements_the_interface_explicitly_should_report_handler_matches_path_with_advice()
        {
            // Arrange — an async handler whose ExecuteAsync is an explicit interface member only
            var registry = new QueryHandlerRegistryAsync();
            registry.Register(typeof(TestQueryA), typeof(System.Guid), typeof(ExplicitInterfaceAsyncHandler));
            var pipelineValidator = new PipelineValidatorBuilder().WithAsyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one error naming the handler and advising a public implementation
            var finding = result.Errors.Single(e => e.RuleId == HandlerMatchesPath);
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain(nameof(ExplicitInterfaceAsyncHandler));
            finding.Message.ShouldContain("public");
        }
    }
}
