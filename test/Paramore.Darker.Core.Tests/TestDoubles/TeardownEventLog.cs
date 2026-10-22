using System.Collections.Generic;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// An ordered record of teardown events, shared by the factories and the scope handle so a test can
    /// assert the order in which they happened.
    /// </summary>
    internal sealed class TeardownEventLog
    {
        public const string HANDLER_RELEASED = "handler released";
        public const string DECORATOR_RELEASED = "decorator released";
        public const string SCOPE_DISPOSE = "scope Dispose";
        public const string SCOPE_DISPOSE_ASYNC = "scope DisposeAsync";

        private readonly List<string> _events = new List<string>();

        public IReadOnlyList<string> Events => _events;

        public void Record(string teardownEvent) => _events.Add(teardownEvent);
    }
}
