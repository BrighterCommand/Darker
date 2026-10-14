using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    // Results are plain strings and items, so these queries need no logger-isolating result types.

    public sealed class DisposableHandlerQuery : IQuery<string>
    {
    }

    public sealed class DisposableHandlerStreamQuery : IStreamQuery<string>
    {
    }

    public sealed class DisposableTrackedHandler : QueryHandler<DisposableHandlerQuery, string>, IDisposableTrackedHandler
    {
        public DisposableTrackedHandler(DisposableHandlerLog log) => log.Record(this);

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;

        public override string Execute(DisposableHandlerQuery query) => "sync";
    }

    public sealed class DisposableTrackedHandlerAsync : QueryHandlerAsync<DisposableHandlerQuery, string>, IDisposableTrackedHandler
    {
        public DisposableTrackedHandlerAsync(DisposableHandlerLog log) => log.Record(this);

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;

        public override Task<string> ExecuteAsync(DisposableHandlerQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult("async");
    }

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
