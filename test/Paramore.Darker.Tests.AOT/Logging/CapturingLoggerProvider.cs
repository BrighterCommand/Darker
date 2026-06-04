// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Paramore.Darker.Tests.AOT.Logging
{
    /// <summary>
    /// A captured structured-log entry: the message template (the <c>{OriginalFormat}</c> value)
    /// and the full structured-argument key/value collection that <see cref="ILogger.Log"/> received.
    /// </summary>
    internal sealed record CapturedLogEntry(
        string? MessageTemplate,
        IReadOnlyList<KeyValuePair<string, object?>> Arguments);

    /// <summary>
    /// A reflection-free, AOT-safe <see cref="ILoggerProvider"/> that appends every structured log
    /// entry to a shared buffer so the harness can read back the decorator's <c>{Query}</c> argument.
    /// </summary>
    internal sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly List<CapturedLogEntry> _entries;

        public CapturingLoggerProvider(List<CapturedLogEntry> entries) => _entries = entries;

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(_entries);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger : ILogger
        {
            private readonly List<CapturedLogEntry> _entries;

            public CapturingLogger(List<CapturedLogEntry> entries) => _entries = entries;

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                if (state is IReadOnlyList<KeyValuePair<string, object?>> arguments)
                {
                    string? template = null;
                    foreach (var argument in arguments)
                    {
                        if (argument.Key == "{OriginalFormat}")
                            template = argument.Value as string;
                    }

                    _entries.Add(new CapturedLogEntry(template, arguments));
                }
            }

            private sealed class NullScope : IDisposable
            {
                public static readonly NullScope Instance = new();

                public void Dispose()
                {
                }
            }
        }
    }
}
