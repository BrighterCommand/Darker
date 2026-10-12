using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A disposable that records, into a shared list, the order in which it was disposed asynchronously.
    /// Its <see cref="Dispose"/> exists only to satisfy tracking and counts separately, so a test can
    /// tell which path tore it down.
    /// </summary>
    internal class RecordingAsyncDisposable : IDisposable, IAsyncDisposable
    {
        private readonly List<string> _disposalLog;

        public RecordingAsyncDisposable(string name, List<string> disposalLog)
        {
            Name = name;
            _disposalLog = disposalLog;
        }

        public string Name { get; }

        public int DisposeAsyncCount { get; private set; }

        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;

        public virtual async ValueTask DisposeAsync()
        {
            await Task.Yield();
            DisposeAsyncCount++;
            _disposalLog.Add(Name);
        }
    }
}
