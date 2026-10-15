using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// An <see cref="IAmAServiceProviderScope"/> whose <see cref="Services"/> is null, and which records
    /// each disposal so a test can prove Darker never disposed it.
    /// </summary>
    public sealed class NullServicesScope : IAmAServiceProviderScope
    {
        private int _disposeCount;
        private int _disposeAsyncCount;

        public IServiceProvider Services => null;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public int DisposeAsyncCount => Volatile.Read(ref _disposeAsyncCount);

        public void Dispose() => Interlocked.Increment(ref _disposeCount);

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeAsyncCount);
            return default;
        }
    }
}
