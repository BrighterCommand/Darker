using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class DisposableTrackedStreamHandler : StreamQueryHandler<DisposableHandlerStreamQuery, string>, IDisposableTrackedHandler
    {
        public DisposableTrackedStreamHandler(DisposableHandlerLog log) => log.Record(this);

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;

        public override async IAsyncEnumerable<string> ExecuteAsync(
            DisposableHandlerStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return "stream";
        }
    }
}
