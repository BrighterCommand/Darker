using System;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// An <see cref="IAmAScope"/> double that counts how often it was disposed, in either form.
    /// </summary>
    internal sealed class RecordingScope : IAmAScope
    {
        public int DisposeCount { get; private set; }

        public int DisposeAsyncCount { get; private set; }

        public void Dispose() => DisposeCount++;

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCount++;
            return default;
        }
    }
}
