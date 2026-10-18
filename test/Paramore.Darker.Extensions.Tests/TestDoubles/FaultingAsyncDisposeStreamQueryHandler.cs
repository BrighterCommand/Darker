using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class FaultingAsyncDisposeStreamQueryHandler
        : StreamQueryHandler<FaultingAsyncDisposeStreamQuery, FaultingAsyncDisposeStreamItem>
    {
        public const string ITEM = "survived-dependency-dispose-async-fault-item";

        private readonly FaultingAsyncDisposeDependency _dependency;

        public FaultingAsyncDisposeStreamQueryHandler(FaultingAsyncDisposeDependency dependency) => _dependency = dependency;

        public override async IAsyncEnumerable<FaultingAsyncDisposeStreamItem> ExecuteAsync(
            FaultingAsyncDisposeStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new FaultingAsyncDisposeStreamItem(_dependency != null ? ITEM : null);
            yield return new FaultingAsyncDisposeStreamItem(ITEM);
        }
    }
}
