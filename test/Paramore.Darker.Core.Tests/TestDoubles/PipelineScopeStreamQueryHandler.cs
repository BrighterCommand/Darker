using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
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
