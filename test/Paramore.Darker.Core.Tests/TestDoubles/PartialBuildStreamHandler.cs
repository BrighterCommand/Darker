using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class PartialBuildStreamHandler : IStreamQueryHandler<PartialBuildStreamQuery, string>
    {
        public IQueryContext Context { get; set; }

        [PassThroughStream(step: 2)]
        [ThrowingInitStream(step: 1)]
        public async IAsyncEnumerable<string> ExecuteAsync(
            PartialBuildStreamQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "never-reached";
            await Task.CompletedTask;
        }
    }
}
