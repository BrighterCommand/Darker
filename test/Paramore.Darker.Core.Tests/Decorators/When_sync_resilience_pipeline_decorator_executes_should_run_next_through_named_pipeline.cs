using System;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies.Handlers;
using Polly;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Decorators
{
    public class When_sync_resilience_pipeline_decorator_executes_should_run_next_through_named_pipeline
    {
        [Fact]
        public void Execute_with_pipeline_in_context_should_run_next_through_named_pipeline()
        {
            // Arrange
            var id = Guid.NewGuid();
            const string pipelineName = "MyPipeline";

            // A real registry with a NON-generic builder registered under the key — proving the
            // shared (useTypePipeline:false) path resolves via the non-generic GetPipeline(key).
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(pipelineName, (builder, _) =>
                builder.AddTimeout(TimeSpan.FromMinutes(1)));

            var context = new QueryContext { ResiliencePipeline = registry };

            var decorator = new UseResiliencePipelineHandler<SyncTestQuery, SyncTestQuery.Result>
            {
                Context = context
            };
            decorator.InitializeFromAttributeParams(new object[] { pipelineName, false });

            var query = new SyncTestQuery(id);
            var expected = new SyncTestQuery.Result { Value = id };

            // Act — the decorator resolves GetPipeline(pipelineName) and runs next through it
            var result = decorator.Execute(query, q => expected, q => null);

            // Assert — the handler result flows back through the named pipeline
            result.ShouldBeSameAs(expected);
        }
    }
}
