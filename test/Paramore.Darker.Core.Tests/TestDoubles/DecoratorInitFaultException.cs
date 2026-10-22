using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A distinctive exception for a decorator that fails while it is being created or initialised.</summary>
    internal sealed class DecoratorInitFaultException : Exception
    {
        public DecoratorInitFaultException(string message) : base(message) { }
    }
}
