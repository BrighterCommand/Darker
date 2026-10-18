using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class DisposableTrackedHandlerAsync : QueryHandlerAsync<DisposableHandlerQuery, string>, IDisposableTrackedHandler
    {
        public DisposableTrackedHandlerAsync(DisposableHandlerLog log) => log.Record(this);

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;

        public override Task<string> ExecuteAsync(DisposableHandlerQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult("async");
    }
}
