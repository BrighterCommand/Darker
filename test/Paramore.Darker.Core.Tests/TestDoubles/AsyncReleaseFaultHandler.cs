using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>An async handler carrying two faulting-on-dispose decorators; its own DisposeAsync faults if given a fault.</summary>
    internal sealed class AsyncReleaseFaultHandler<TTag>
        : QueryHandlerAsync<AsyncReleaseFaultQuery<TTag>, AsyncReleaseFaultResult<TTag>>, IRecordsAsyncDisposal, IAsyncDisposable
    {
        public const string RESULT = "survived-async-release-faults";

        private readonly FaultingAsyncDisposal _disposal;

        public AsyncReleaseFaultHandler(Exception fault) => _disposal = new FaultingAsyncDisposal(fault);

        public int DisposeAsyncCount => _disposal.Count;

        [FaultOnDisposeAsync(step: 1)]
        [FaultOnDisposeAsync(step: 2)]
        public override Task<AsyncReleaseFaultResult<TTag>> ExecuteAsync(
            AsyncReleaseFaultQuery<TTag> query, CancellationToken cancellationToken = default)
            => Task.FromResult(new AsyncReleaseFaultResult<TTag>(RESULT));

        public ValueTask DisposeAsync() => _disposal.DisposeAsync();
    }
}
