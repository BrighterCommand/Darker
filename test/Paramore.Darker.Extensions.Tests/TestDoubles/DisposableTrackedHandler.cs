namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class DisposableTrackedHandler : QueryHandler<DisposableHandlerQuery, string>, IDisposableTrackedHandler
    {
        public DisposableTrackedHandler(DisposableHandlerLog log) => log.Record(this);

        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;

        public override string Execute(DisposableHandlerQuery query) => "sync";
    }
}
