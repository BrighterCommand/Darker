using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A handler that throws from <see cref="Execute"/> and again from <see cref="Dispose"/>.</summary>
    internal sealed class ThrowingExecuteAndDisposeQueryHandler
        : QueryHandler<ThrowingExecuteAndDisposeQuery, ThrowingExecuteAndDisposeResult>, IDisposable
    {
        private readonly Exception _executeFault;
        private readonly Exception _disposeFault;

        public ThrowingExecuteAndDisposeQueryHandler(Exception executeFault, Exception disposeFault)
        {
            _executeFault = executeFault;
            _disposeFault = disposeFault;
        }

        public override ThrowingExecuteAndDisposeResult Execute(ThrowingExecuteAndDisposeQuery query)
            => throw _executeFault;

        public void Dispose() => throw _disposeFault;
    }
}
