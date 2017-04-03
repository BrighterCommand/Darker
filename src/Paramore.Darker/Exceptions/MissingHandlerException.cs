using System;

namespace Paramore.Darker.Exceptions
{
    public sealed class MissingHandlerException : Exception
    {
        public MissingHandlerException(string message) : base(message)
        {
        }
    }
}