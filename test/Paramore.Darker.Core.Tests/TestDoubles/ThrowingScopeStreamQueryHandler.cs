using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class ThrowingScopeStreamQueryHandler : StreamQueryHandler<ThrowingScopeStreamQuery, ThrowingScopeStreamItem>
    {
        public const string ITEM = "throwing-scope-stream";

        public override async IAsyncEnumerable<ThrowingScopeStreamItem> ExecuteAsync(
            ThrowingScopeStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new ThrowingScopeStreamItem(ITEM);
        }
    }
}
