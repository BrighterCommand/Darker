using System;
using System.Collections.Generic;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A disposable that records, into a shared list, the order in which it was disposed.
    /// </summary>
    internal class RecordingDisposable : IDisposable
    {
        private readonly List<string> _disposalLog;

        public RecordingDisposable(string name, List<string> disposalLog)
        {
            Name = name;
            _disposalLog = disposalLog;
        }

        public string Name { get; }

        public int DisposeCount { get; private set; }

        public virtual void Dispose()
        {
            DisposeCount++;
            _disposalLog.Add(Name);
        }
    }
}
