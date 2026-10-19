using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// An <see cref="IAmAScope"/> that exposes no service provider, and records each disposal so a test
    /// can prove Darker never disposed it.
    /// </summary>
    public sealed class PlainRecordingScope : IAmAScope
    {
        private int _disposeCount;
        private int _disposeAsyncCount;

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
