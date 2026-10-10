using System;
using Microsoft.Extensions.Logging;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A logger provider whose loggers throw when asked to write at <see cref="LogLevel.Error"/>,
    /// to prove that a faulty logging provider cannot break teardown.
    /// </summary>
    public sealed class ThrowingLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new ThrowingLogger();

        public void Dispose()
        {
        }

        private sealed class ThrowingLogger : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                if (logLevel == LogLevel.Error)
                    throw new InvalidOperationException("the logging provider failed");
            }
        }
    }
}
