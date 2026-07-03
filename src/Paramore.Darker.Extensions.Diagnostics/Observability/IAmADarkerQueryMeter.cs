#region Licence

/* The MIT License (MIT)
Copyright © 2025 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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
/// Meter for generating query duration metrics from traces following the metrics-from-traces
/// pattern (ADR 0018). Implementations record measurements from query activity spans onto the
/// <c>paramore.darker.query.duration</c> histogram.
/// </summary>
public interface IAmADarkerQueryMeter
{
    /// <summary>
    /// Records the duration of a query operation from a completed activity span.
    /// Only low-cardinality allowed tags are forwarded as metric dimensions.
    /// </summary>
    /// <param name="activity">The stopped activity representing the query span.</param>
    void RecordQueryOperation(Activity activity);

    /// <summary>
    /// Gets a value indicating whether any listeners are subscribed to the query duration meter.
    /// Returns <c>false</c> when no <see cref="OpenTelemetry.Metrics.MeterProvider"/> has
    /// registered the meter, enabling cheap short-circuiting (NFR2).
    /// </summary>
    bool Enabled { get; }
}
