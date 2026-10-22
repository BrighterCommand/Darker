using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class FaultingAsyncDisposeQueryHandler : QueryHandlerAsync<FaultingAsyncDisposeQuery, FaultingAsyncDisposeResult>
    {
        public const string RESULT = "survived-dependency-dispose-async-fault";

        private readonly FaultingAsyncDisposeDependency _dependency;

        public FaultingAsyncDisposeQueryHandler(FaultingAsyncDisposeDependency dependency) => _dependency = dependency;

        public override Task<FaultingAsyncDisposeResult> ExecuteAsync(FaultingAsyncDisposeQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(new FaultingAsyncDisposeResult(_dependency != null ? RESULT : null));
    }
}
