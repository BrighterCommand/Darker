using System;

namespace Darker.Exceptions
{
    public sealed class ConfigurationException : Exception
    {
        public ConfigurationException(string message) : base(message)
        {
        }
    }
}