using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class StreamLifetimeFaultQueryHandler : StreamQueryHandler<StreamLifetimeFaultQuery, StreamLifetimeFaultItem>
    {
        public const string ITEM = "stream-lifetime-fault";

        public override async IAsyncEnumerable<StreamLifetimeFaultItem> ExecuteAsync(
            StreamLifetimeFaultQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new StreamLifetimeFaultItem(ITEM);
        }
    }
}
