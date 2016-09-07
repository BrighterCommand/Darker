using System;

namespace Darker.Exceptions
{
    public sealed class MissingHandlerDecoratorException : Exception
    {
        public MissingHandlerDecoratorException(string message) : base(message)
        {
        }
    }
}