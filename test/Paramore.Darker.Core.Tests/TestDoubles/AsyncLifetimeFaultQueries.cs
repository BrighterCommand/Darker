using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    // Each path has a result type of its own, so the static per-TResult logger in the pipeline is not shared.

    internal sealed class AsyncLifetimeFaultQuery : IQuery<AsyncLifetimeFaultResult>
    {
    }

    internal sealed class AsyncLifetimeFaultResult
    {
        public AsyncLifetimeFaultResult(string value) => Value = value;

        public string Value { get; }
    }

    internal sealed class AsyncLifetimeFaultQueryHandler : QueryHandlerAsync<AsyncLifetimeFaultQuery, AsyncLifetimeFaultResult>
    {
        public const string RESULT = "async-lifetime-fault";

        public override Task<AsyncLifetimeFaultResult> ExecuteAsync(AsyncLifetimeFaultQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(new AsyncLifetimeFaultResult(RESULT));
    }

    internal sealed class StreamLifetimeFaultQuery : IStreamQuery<StreamLifetimeFaultItem>
    {
    }

    internal sealed class StreamLifetimeFaultItem
    {
        public StreamLifetimeFaultItem(string value) => Value = value;

        public string Value { get; }
    }

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
