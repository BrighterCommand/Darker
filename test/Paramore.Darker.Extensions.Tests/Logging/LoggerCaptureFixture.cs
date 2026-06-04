using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Extensions.Tests.Logging;
using Paramore.Darker.Logging;
using Xunit;

// Assembly-scoped install (FR10 install-before-touch). This fixture lives in a disjoint assembly
// from the Core.Tests one and serves the disjoint QueryLoggingDecorator<ExtensionsLoggingTestQuery, …>
// closed generic — each test assembly runs in its own process, so the ApplicationLogging.LoggerFactory
// globals do not interfere.
[assembly: AssemblyFixture(typeof(LoggerCaptureFixture))]

namespace Paramore.Darker.Extensions.Tests.Logging
{
    /// <summary>
    /// Assembly-scoped fixture that swaps <see cref="ApplicationLogging.LoggerFactory"/> for one that
    /// captures every <see cref="ILogger"/> entry into an in-memory buffer. Restores the previous
    /// factory on disposal.
    /// </summary>
    public sealed class LoggerCaptureFixture : IDisposable
    {
        private readonly ILoggerFactory _previousFactory;
        private readonly CapturingLoggerProvider _provider;

        public LoggerCaptureFixture()
        {
            _previousFactory = ApplicationLogging.LoggerFactory;
            _provider = new CapturingLoggerProvider();
            ApplicationLogging.LoggerFactory = new LoggerFactory(new ILoggerProvider[] { _provider });
        }

        /// <summary>A snapshot of the log entries captured so far.</summary>
        public IReadOnlyList<CapturedLogEntry> CapturedLogs => _provider.Snapshot();

        /// <summary>
        /// The capturing provider, exposed so DI-based tests can register it into the container's
        /// logging — <c>AddDarker</c> overwrites <see cref="ApplicationLogging.LoggerFactory"/> from
        /// the container's <see cref="ILoggerFactory"/>, so the capture must flow through DI there.
        /// </summary>
        public ILoggerProvider Provider => _provider;

        /// <summary>Clears the captured buffer so a single test asserts in isolation.</summary>
        public void Clear() => _provider.Clear();

        public void Dispose() => ApplicationLogging.LoggerFactory = _previousFactory;
    }

    /// <summary>An <see cref="ILoggerProvider"/> whose loggers record entries into a shared buffer.</summary>
    public sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<CapturedLogEntry> _entries = new List<CapturedLogEntry>();
        private readonly object _gate = new object();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

        public void Record(CapturedLogEntry entry)
        {
            lock (_gate)
            {
                _entries.Add(entry);
            }
        }

        public IReadOnlyList<CapturedLogEntry> Snapshot()
        {
            lock (_gate)
            {
                return _entries.ToArray();
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _entries.Clear();
            }
        }

        public void Dispose()
        {
        }
    }

    /// <summary>An <see cref="ILogger"/> that records each log call as a <see cref="CapturedLogEntry"/>.</summary>
    public sealed class CapturingLogger : ILogger
    {
        private readonly CapturingLoggerProvider _provider;

        public CapturingLogger(CapturingLoggerProvider provider) => _provider = provider;

        public IDisposable BeginScope<TState>(TState state) => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            var arguments = state as IReadOnlyList<KeyValuePair<string, object>>;

            string messageTemplate = null;
            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    if (argument.Key == "{OriginalFormat}")
                    {
                        messageTemplate = argument.Value as string;
                        break;
                    }
                }
            }

            var rendered = formatter != null ? formatter(state, exception) : state?.ToString();
            var copiedArguments = arguments?.ToArray() ?? Array.Empty<KeyValuePair<string, object>>();

            _provider.Record(new CapturedLogEntry(logLevel, messageTemplate, rendered, copiedArguments, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new NullScope();

            public void Dispose()
            {
            }
        }
    }

    /// <summary>A single captured log entry, retaining both the message template and the structured arguments.</summary>
    public sealed record CapturedLogEntry(
        LogLevel LogLevel,
        string MessageTemplate,
        string RenderedMessage,
        IReadOnlyList<KeyValuePair<string, object>> StructuredArguments,
        Exception Exception);
}
