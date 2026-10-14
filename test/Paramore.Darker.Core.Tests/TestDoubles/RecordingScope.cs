using System;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// An <see cref="IAmAScope"/> double that counts how often it was disposed, in either form.
    /// </summary>
    internal sealed class RecordingScope : IAmAScope
    {
        private readonly TeardownEventLog _log;

        public RecordingScope(TeardownEventLog log = null) => _log = log;

        public int DisposeCount { get; private set; }

        public int DisposeAsyncCount { get; private set; }

        public void Dispose()
        {
            DisposeCount++;
            _log?.Record(TeardownEventLog.SCOPE_DISPOSE);
        }

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCount++;
            _log?.Record(TeardownEventLog.SCOPE_DISPOSE_ASYNC);
            return default;
        }
    }
}
