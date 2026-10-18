using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class DisposalRecord
    {
        public string DisposedBy { get; private set; }

        public void Sync() => DisposedBy = nameof(IDisposable.Dispose);

        public void Async() => DisposedBy = nameof(IAsyncDisposable.DisposeAsync);
    }
}
