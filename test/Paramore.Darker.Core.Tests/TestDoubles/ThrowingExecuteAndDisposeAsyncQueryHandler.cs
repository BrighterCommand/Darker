using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
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
}
