using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    // One query and handler per path, each carrying a single pass-through decorator, so that both the
    // handler factory and the decorator factory are asked to create a component.

    internal sealed class PipelineScopeQuery : IQuery<string>
    {
    }

    internal sealed class PipelineScopeQueryHandler : QueryHandler<PipelineScopeQuery, string>
    {
        public const string RESULT = "pipeline-scope-sync";

        [FirstPassThrough(step: 1)]
        public override string Execute(PipelineScopeQuery query) => RESULT;
    }

    internal sealed class PipelineScopeAsyncQuery : IQuery<string>
    {
    }

    internal sealed class PipelineScopeAsyncQueryHandler : QueryHandlerAsync<PipelineScopeAsyncQuery, string>
    {
        public const string RESULT = "pipeline-scope-async";

        [PassThroughAsync(step: 1)]
        public override Task<string> ExecuteAsync(PipelineScopeAsyncQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(RESULT);
    }

    internal sealed class PipelineScopeStreamQuery : IStreamQuery<string>
    {
    }

    internal sealed class PipelineScopeStreamQueryHandler : StreamQueryHandler<PipelineScopeStreamQuery, string>
    {
        public const string ITEM = "pipeline-scope-stream";

        [PassThroughStream(step: 1)]
        public override async IAsyncEnumerable<string> ExecuteAsync(
            PipelineScopeStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return ITEM;
        }
    }
}
