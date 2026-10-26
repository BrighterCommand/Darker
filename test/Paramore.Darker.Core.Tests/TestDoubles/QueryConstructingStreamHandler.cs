using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A stream handler whose ExecuteAsync carries an attribute naming a decorator that closes over a concrete query class only.
    /// </summary>
    internal class QueryConstructingStreamHandler : IStreamQueryHandler<StreamTestQuery, string>
    {
        public IQueryContext Context { get; set; }

        [QueryConstructingStream]
        public async IAsyncEnumerable<string> ExecuteAsync(
            StreamTestQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "item";
            await Task.CompletedTask;
        }
    }
}
