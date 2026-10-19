using System;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>Counts a DisposeAsync that completes only after a yield, then throws the fault it was given (if any).</summary>
    internal sealed class FaultingAsyncDisposal
    {
        private readonly Exception _fault;

        public FaultingAsyncDisposal(Exception fault) => _fault = fault;

        public int Count { get; private set; }

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            Count++;
            if (_fault != null)
                throw _fault;
        }
    }
}
