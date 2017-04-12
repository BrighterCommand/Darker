using System;

namespace Paramore.Darker.Exceptions
{
    public sealed class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message)
        {
        }
    }
}