using System;

namespace Darker.Exceptions
{
    internal sealed class FallbackException : Exception
    {
        public FallbackException(Exception innerException) : base(string.Empty, innerException)
        {
        }
    }
}