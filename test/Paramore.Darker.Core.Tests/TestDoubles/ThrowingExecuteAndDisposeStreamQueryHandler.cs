using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A stream handler that throws during enumeration and again from <see cref="DisposeAsync"/>.</summary>
    internal sealed class ThrowingExecuteAndDisposeStreamQueryHandler
        : IStreamQueryHandler<ThrowingExecuteAndDisposeStreamQuery, ThrowingExecuteAndDisposeStreamItem>, IAsyncDisposable
    {
        private readonly Exception _executeFault;
        private readonly Exception _disposeFault;

        public ThrowingExecuteAndDisposeStreamQueryHandler(Exception executeFault, Exception disposeFault)
        {
            _executeFault = executeFault;
            _disposeFault = disposeFault;
        }

        public IQueryContext Context { get; set; }

        public async IAsyncEnumerable<ThrowingExecuteAndDisposeStreamItem> ExecuteAsync(
            ThrowingExecuteAndDisposeStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new ThrowingExecuteAndDisposeStreamItem();
            throw _executeFault;
        }

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            throw _disposeFault;
        }
    }
}
