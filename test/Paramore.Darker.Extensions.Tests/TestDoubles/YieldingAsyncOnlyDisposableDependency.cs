using System;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A service that implements only <see cref="IAsyncDisposable"/> and whose DisposeAsync completes
    /// only after an awaited yield, so a caller that does not await it observes the flag unset.
    /// </summary>
    public sealed class YieldingAsyncOnlyDisposableDependency : IAsyncDisposable
    {
        private readonly AsyncDisposeProbe _probe;

        public YieldingAsyncOnlyDisposableDependency(AsyncDisposeProbe probe) => _probe = probe;

        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            _probe.DisposedAsync = true;
        }
    }
}
