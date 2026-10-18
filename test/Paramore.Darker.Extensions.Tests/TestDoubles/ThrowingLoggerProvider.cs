using System;
using Microsoft.Extensions.Logging;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A logger provider whose loggers throw when asked to write at one level (<see cref="LogLevel.Error"/> by default),
    /// to prove that a faulty logging provider cannot break teardown.
    /// </summary>
    public sealed class ThrowingLoggerProvider(LogLevel throwAt = LogLevel.Error, bool armed = true) : ILoggerProvider
    {
        private volatile bool _armed = armed;

        /// <summary>Starts throwing. A provider created unarmed lets the pipeline build quietly, so the fault lands in teardown.</summary>
        public void Arm() => _armed = true;

        public ILogger CreateLogger(string categoryName) => new ThrowingLogger(throwAt, () => _armed);

        public void Dispose()
        {
        }

        private sealed class ThrowingLogger(LogLevel throwAt, Func<bool> armed) : ILogger
        {
            public IDisposable BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                if (armed() && logLevel == throwAt)
                    throw new InvalidOperationException("the logging provider failed");
            }
        }
    }
}
