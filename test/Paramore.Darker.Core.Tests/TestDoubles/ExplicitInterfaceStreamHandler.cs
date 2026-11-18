using System.Collections.Generic;
using System.Threading;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A stream handler that implements the interface's ExecuteAsync only explicitly, so it has no public ExecuteAsync.</summary>
    internal sealed class ExplicitInterfaceStreamHandler : IStreamQueryHandler<StreamTestQuery, string>
    {
        public IQueryContext Context { get; set; }

#pragma warning disable CS1998
        async IAsyncEnumerable<string> IStreamQueryHandler<StreamTestQuery, string>.ExecuteAsync(StreamTestQuery query, CancellationToken cancellationToken)
#pragma warning restore CS1998
        {
            yield return "item";
        }
    }
}
