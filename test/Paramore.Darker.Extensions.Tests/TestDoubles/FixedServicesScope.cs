using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// An <see cref="IAmAServiceProviderScope"/> that exposes exactly the provider it is given, with no scope
    /// of its own, and records each disposal so a test can prove Darker never disposed it.
    /// </summary>
    public sealed class FixedServicesScope(IServiceProvider services) : IAmAServiceProviderScope
    {
        private int _disposeCount;
        private int _disposeAsyncCount;

        public IServiceProvider Services => services;

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
