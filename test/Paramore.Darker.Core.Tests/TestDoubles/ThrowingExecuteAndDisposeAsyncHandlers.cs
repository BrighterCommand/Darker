using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    // The async path and the stream path have result types of their own.

    internal sealed class ThrowingExecuteAndDisposeAsyncQuery : IQuery<ThrowingExecuteAndDisposeAsyncResult>
    {
    }

    internal sealed class ThrowingExecuteAndDisposeAsyncResult
    {
    }

    /// <summary>A handler that throws from <see cref="ExecuteAsync"/> and again from <see cref="DisposeAsync"/>.</summary>
    internal sealed class ThrowingExecuteAndDisposeAsyncQueryHandler
        : QueryHandlerAsync<ThrowingExecuteAndDisposeAsyncQuery, ThrowingExecuteAndDisposeAsyncResult>, IAsyncDisposable
    {
        private readonly Exception _executeFault;
        private readonly Exception _disposeFault;

        public ThrowingExecuteAndDisposeAsyncQueryHandler(Exception executeFault, Exception disposeFault)
        {
            _executeFault = executeFault;
            _disposeFault = disposeFault;
        }

        public override Task<ThrowingExecuteAndDisposeAsyncResult> ExecuteAsync(
            ThrowingExecuteAndDisposeAsyncQuery query, CancellationToken cancellationToken = default)
            => throw _executeFault;

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            throw _disposeFault;
        }
    }

    internal sealed class ThrowingExecuteAndDisposeStreamQuery : IStreamQuery<ThrowingExecuteAndDisposeStreamItem>
    {
    }

    internal sealed class ThrowingExecuteAndDisposeStreamItem
    {
    }

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
