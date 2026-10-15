using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// An ambient scope as a third-party adapter might offer it: it exposes the services of an
    /// <see cref="IServiceScope"/> and, unlike a borrowed handle, really disposes that scope when it is
    /// disposed. It records each disposal so a test can prove Darker never disposed it.
    /// </summary>
    public sealed class RecordingAmbientScope(IServiceScope scope) : IAmAServiceProviderScope
    {
        private int _disposeCount;
        private int _disposeAsyncCount;

        public IServiceProvider Services => scope.ServiceProvider;

        public int DisposeCount => Volatile.Read(ref _disposeCount);

        public int DisposeAsyncCount => Volatile.Read(ref _disposeAsyncCount);

        public void Dispose()
        {
            Interlocked.Increment(ref _disposeCount);
            scope.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Interlocked.Increment(ref _disposeAsyncCount);
            if (scope is IAsyncDisposable asyncDisposable)
                return asyncDisposable.DisposeAsync();

            scope.Dispose();
            return default;
        }
    }
}
