using System;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A service that implements only <see cref="IAsyncDisposable"/> and whose DisposeAsync faults.</summary>
    public sealed class FaultingAsyncDisposeDependency : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            throw new DependencyDisposeAsyncFaultException();
        }
    }
}
