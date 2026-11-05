using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A stream handler whose ExecuteAsync carries two decorator attributes at the same Step.</summary>
    internal class DuplicateStepStreamHandler : IStreamQueryHandler<StreamTestQuery, string>
    {
        public IQueryContext Context { get; set; }

        [PassThroughStream(2)]
        [StreamStepEvent(step: 2)]
        public async IAsyncEnumerable<string> ExecuteAsync(
            StreamTestQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "item";
            await Task.CompletedTask;
        }
    }
}
