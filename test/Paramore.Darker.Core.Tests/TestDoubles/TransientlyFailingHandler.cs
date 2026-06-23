using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A counting test double that throws on its first <c>failuresBeforeSuccess</c> invocations and
    /// succeeds thereafter. Used to prove a retry resilience pipeline retries a transient failure to
    /// success. Deterministic — failure is driven purely by the call count, not time.
    /// </summary>
    internal sealed class TransientlyFailingHandler
    {
        private readonly int _failuresBeforeSuccess;

        public TransientlyFailingHandler(int failuresBeforeSuccess)
        {
            _failuresBeforeSuccess = failuresBeforeSuccess;
        }

        public int Calls { get; private set; }

        public SyncTestQuery.Result Execute(SyncTestQuery query)
        {
            Calls++;
            if (Calls <= _failuresBeforeSuccess)
                throw new InvalidOperationException("transient failure");

            return new SyncTestQuery.Result { Value = query.Id };
        }

        public Task<SyncTestQuery.Result> ExecuteAsync(SyncTestQuery query, CancellationToken cancellationToken)
        {
            Calls++;
            if (Calls <= _failuresBeforeSuccess)
                throw new InvalidOperationException("transient failure");

            return Task.FromResult(new SyncTestQuery.Result { Value = query.Id });
        }
    }
}
