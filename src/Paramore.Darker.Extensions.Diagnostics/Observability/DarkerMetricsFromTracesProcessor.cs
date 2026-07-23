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
using OpenTelemetry;
using Paramore.Darker.Observability;

namespace Paramore.Darker.Extensions.Diagnostics.Observability;

/// <summary>
/// Generates metrics from traces following OpenTelemetry's metrics-from-traces pattern (ADR 0018).
/// On each span end, filters to the <c>paramore.darker</c> source and dispatches to the appropriate
/// meter by <see cref="ActivityKind"/>: <see cref="ActivityKind.Internal"/> (query spans) ⇒
/// <see cref="IAmADarkerQueryMeter"/> and <see cref="IAmADarkerCacheMeter"/>;
/// <see cref="ActivityKind.Client"/> (DB spans) ⇒ <see cref="IAmADarkerDbMeter"/>.
/// Holds no metric state of its own.
/// </summary>
/// <remarks>
/// Short-circuits cheaply when no meter has listeners (NFR2). The processor is added to
/// the tracer pipeline only when meters are registered, so there is no cost when the user
/// wires only tracing.
/// </remarks>
public sealed class DarkerMetricsFromTracesProcessor(
    IAmADarkerTracer tracer,
    IAmADarkerQueryMeter queryMeter,
    IAmADarkerDbMeter dbMeter,
    IAmADarkerCacheMeter cacheMeter)
    : BaseProcessor<Activity>
{
    private readonly string _sourceName = tracer.ActivitySource.Name;

    /// <inheritdoc />
    public override void OnEnd(Activity? activity)
    {
        if (!(queryMeter.Enabled || dbMeter.Enabled || cacheMeter.Enabled)) return;

        if (activity is null) return;

        if (activity.Source.Name != _sourceName) return;

        switch (activity.Kind)
        {
            case ActivityKind.Internal:
                queryMeter.RecordQueryOperation(activity);
                cacheMeter.RecordCacheOperation(activity);
                break;
            case ActivityKind.Client:
                dbMeter.RecordClientOperation(activity);
                break;
        }

        base.OnEnd(activity);
    }
}
