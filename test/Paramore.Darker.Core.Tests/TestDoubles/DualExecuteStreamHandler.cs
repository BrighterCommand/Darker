using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A stream handler that ALSO exposes a <see cref="Task{TResult}"/> ExecuteAsync overload
    /// with a broader parameter type, causing GetMethod("ExecuteAsync") without type args to throw
    /// AmbiguousMatchException. BuildStream must resolve by signature, not bare name.
    /// </summary>
    internal class DualExecuteStreamHandler : IStreamQueryHandler<StreamTestQuery, string>
    {
        public static readonly string[] Items = { "alpha", "beta", "gamma" };

        public IQueryContext Context { get; set; }

        // Broad-parameter overload: makes bare GetMethod("ExecuteAsync") ambiguous.
        public Task<string> ExecuteAsync(IStreamQuery<string> query, CancellationToken cancellationToken = default)
            => Task.FromResult("wrong");

#pragma warning disable CS1998
        public async IAsyncEnumerable<string> ExecuteAsync(StreamTestQuery query, CancellationToken cancellationToken = default)
#pragma warning restore CS1998
        {
            foreach (var item in Items)
                yield return item;
        }
    }
}
