using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A handler that records whether the container disposed it.
    /// </summary>
    public interface IDisposableTrackedHandler : IDisposable
    {
        bool IsDisposed { get; }
    }
}
