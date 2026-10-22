#region Licence
/* The MIT License (MIT)
Copyright © 2024 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */
#endregion

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Logging;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// Reports why Darker did not adopt an ambient scope, logging each reason at most once for the
    /// container that holds this singleton. The logger is created at first use, after
    /// <see cref="ApplicationLogging.LoggerFactory"/> has been set from the container.
    /// </summary>
    internal sealed class AdoptionDiagnostics
    {
        private readonly ConcurrentDictionary<string, byte> _reported = new();
        private ILogger _logger;

        public void WarnOnce(string reason, string message)
        {
            // One atomic add decides which of any concurrent callers logs.
            if (!_reported.TryAdd(reason, 0))
                return;

            _logger ??= ApplicationLogging.CreateLogger<AdoptionDiagnostics>();
            _logger.LogWarning("{Message}", message);
        }
    }
}
