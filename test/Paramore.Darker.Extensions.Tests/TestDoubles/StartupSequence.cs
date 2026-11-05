using System.Collections.Generic;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A thread-safe recorder of the order in which startup steps happen.</summary>
    public sealed class StartupSequence
    {
        private readonly object _gate = new();
        private readonly List<string> _entries = new();

        /// <summary>A snapshot of the entries recorded so far.</summary>
        public string[] Entries
        {
            get
            {
                lock (_gate)
                    return _entries.ToArray();
            }
        }

        /// <summary>Records one step.</summary>
        public void Record(string entry)
        {
            lock (_gate)
                _entries.Add(entry);
        }
    }
}
