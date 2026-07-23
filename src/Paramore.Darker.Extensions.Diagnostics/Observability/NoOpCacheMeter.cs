#region Licence

/* The MIT License (MIT)
Copyright © 2026 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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

using System.Diagnostics;

namespace Paramore.Darker.Extensions.Diagnostics.Observability;

/// <summary>
/// No-op implementation of <see cref="IAmADarkerCacheMeter"/> registered when cache-metrics
/// emission is disabled via the <c>emitCacheMetrics: false</c> toggle on
/// <see cref="DarkerMetricsBuilderExtensions.AddDarkerInstrumentation"/>. Records nothing and
/// reports <c>Enabled = false</c>, so the short-circuit guard in
/// <see cref="DarkerMetricsFromTracesProcessor"/> eliminates all overhead (FR10, ADR 0021).
/// </summary>
public sealed class NoOpCacheMeter : IAmADarkerCacheMeter
{
    /// <inheritdoc />
    public void RecordCacheOperation(Activity activity) { }

    /// <inheritdoc />
    public bool Enabled => false;
}
