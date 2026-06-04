using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Core.Tests.Logging;
using Paramore.Darker.Logging;
using Xunit;

// Install the capturing LoggerFactory before any test in this assembly runs, so it is in place
// before any QueryLoggingDecorator<,> closed generic caches its static readonly Logger field
// (the "install-before-touch" invariant of FR10). xunit.v3 3.2.2 exposes this through the
// AssemblyFixtureAttribute rather than the IAssemblyFixture<T> marker the spec assumed; the
// attribute's "initialized before any test in the assembly are run" contract is the equivalent
// (and stronger) guarantee.
[assembly: AssemblyFixture(typeof(LoggerCaptureFixture))]

namespace Paramore.Darker.Core.Tests.Logging
{
    /// <summary>
    /// Assembly-scoped fixture that swaps <see cref="ApplicationLogging.LoggerFactory"/> for one that
    /// captures every <see cref="ILogger"/> entry into an in-memory buffer, so tests can assert on the
    /// query logging decorator's output. Restores the previous factory on disposal.
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
