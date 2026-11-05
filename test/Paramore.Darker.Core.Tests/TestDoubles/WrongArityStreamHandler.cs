using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A stream handler whose ExecuteAsync carries an attribute naming a decorator that cannot be closed.
    /// </summary>
    internal class WrongArityStreamHandler : IStreamQueryHandler<StreamTestQuery, string>
    {
        public IQueryContext Context { get; set; }

        [WrongArityStream]
        public async IAsyncEnumerable<string> ExecuteAsync(
            StreamTestQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "item";
            await Task.CompletedTask;
        }
    }
}
