using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class StreamReleaseFaultHandler<TTag>
        : IStreamQueryHandler<StreamReleaseFaultQuery<TTag>, StreamReleaseFaultItem<TTag>>, IRecordsAsyncDisposal, IAsyncDisposable
    {
        public const string ITEM = "survived-stream-release-faults";

        private readonly FaultingAsyncDisposal _disposal;

        public StreamReleaseFaultHandler(Exception fault) => _disposal = new FaultingAsyncDisposal(fault);

        public IQueryContext Context { get; set; }

        public int DisposeAsyncCount => _disposal.Count;

        [FaultOnDisposeStream(step: 1)]
        [FaultOnDisposeStream(step: 2)]
        public async IAsyncEnumerable<StreamReleaseFaultItem<TTag>> ExecuteAsync(
            StreamReleaseFaultQuery<TTag> query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new StreamReleaseFaultItem<TTag>(ITEM);
            yield return new StreamReleaseFaultItem<TTag>(ITEM);
        }

        public ValueTask DisposeAsync() => _disposal.DisposeAsync();
    }
}
