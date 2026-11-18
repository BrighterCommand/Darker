using System.Linq;
using Paramore.Darker.Core.Tests.Exported;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.PipelineValidation
{
    public class SyncHandlerExplicitInterfaceRuleTests
    {
        private static readonly ValidationRuleId HandlerMatchesPath = new("HandlerMatchesPath");

        [Fact]
        public void When_a_sync_handler_implements_the_interface_explicitly_should_report_handler_matches_path_with_advice()
        {
            // Arrange — a sync handler whose Execute is an explicit interface member only
            var registry = new QueryHandlerRegistry();
            registry.Register(typeof(TestQueryA), typeof(System.Guid), typeof(ExplicitInterfaceSyncHandler));
            var pipelineValidator = new PipelineValidatorBuilder().WithSyncRegistry(registry).Build();

            // Act
            var result = pipelineValidator.Validate();

            // Assert — one error naming the handler and advising a public implementation
            var finding = result.Errors.Single(e => e.RuleId == HandlerMatchesPath);
            finding.Severity.ShouldBe(ValidationSeverity.Error);
            finding.Message.ShouldContain(nameof(ExplicitInterfaceSyncHandler));
            finding.Message.ShouldContain("'IQueryHandler<,>' only explicitly");
            finding.Message.ShouldContain("public");
        }
    }
}
