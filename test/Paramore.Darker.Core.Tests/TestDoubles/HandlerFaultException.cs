using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A distinctive exception, so a test can tell the handler's fault from any other.</summary>
    internal sealed class HandlerFaultException : Exception
    {
        public HandlerFaultException(string message) : base(message) { }
    }
}
