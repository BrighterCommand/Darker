using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class MultiItemStreamQuery : IStreamQuery<string>
    {
    }

    internal class MultiItemStreamHandler : IStreamQueryHandler<MultiItemStreamQuery, string>
    {
        public static readonly string[] Items = { "first", "second", "third" };

        public IQueryContext Context { get; set; }

        public async IAsyncEnumerable<string> ExecuteAsync(
            MultiItemStreamQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var item in Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
            await System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
