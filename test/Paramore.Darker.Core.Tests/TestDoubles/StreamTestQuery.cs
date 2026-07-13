using System.Collections.Generic;
using System.Threading;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class StreamTestQuery : IStreamQuery<string>
    {
    }

    internal class StreamTestQueryHandler : IStreamQueryHandler<StreamTestQuery, string>
    {
        public IQueryContext Context { get; set; }

#pragma warning disable CS1998
        public async IAsyncEnumerable<string> ExecuteAsync(StreamTestQuery query, CancellationToken cancellationToken = default)
#pragma warning restore CS1998
        {
            yield return "item";
        }
    }

    internal class StreamTestQueryOfDifferentResult : IStreamQuery<int>
    {
    }
}
