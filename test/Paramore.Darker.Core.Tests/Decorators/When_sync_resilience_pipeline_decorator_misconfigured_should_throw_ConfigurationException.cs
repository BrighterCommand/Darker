using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Policies.Handlers;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Decorators
{
    public class SyncResiliencePipelineDecoratorValidationTests
    {
        [Fact]
        public void When_no_provider_on_context_should_throw_ConfigurationException()
        {
            // Arrange — context has no resilience pipeline provider
            var decorator = new UseResiliencePipelineHandler<SyncTestQuery, SyncTestQuery.Result>
            {
                Context = new QueryContext()
            };

            // Act
            var exception = Should.Throw<ConfigurationException>(() =>
                decorator.InitializeFromAttributeParams(new object[] { "MyPipeline", false }));

            // Assert — failure surfaces at initialization, before any query executes
            exception.Message.ShouldContain("provider");
        }

        [Fact]
        public void When_key_not_registered_should_throw_ConfigurationException_naming_the_key()
        {
            // Arrange — provider present, but the requested key is not registered
            var registry = new ResiliencePipelineRegistry<string>();
            var decorator = new UseResiliencePipelineHandler<SyncTestQuery, SyncTestQuery.Result>
            {
                Context = new QueryContext { ResiliencePipeline = registry }
            };

            // Act
            var exception = Should.Throw<ConfigurationException>(() =>
                decorator.InitializeFromAttributeParams(new object[] { "MissingPipeline", false }));

            // Assert — the message names the unresolved key
            exception.Message.ShouldContain("MissingPipeline");
        }
    }
}
