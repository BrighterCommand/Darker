using System;
using System.Diagnostics;
#if NET8_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Text.Json;
using Paramore.Darker.Logging;

namespace Paramore.Darker.Observability;

/// <summary>
/// Default implementation of <see cref="IAmADarkerTracer"/>. Owns a single
/// <see cref="System.Diagnostics.ActivitySource"/> named <c>paramore.darker</c> and manages the
/// query-span lifecycle. Dispose to release the underlying source.
/// </summary>
/// <remarks>
/// Register this tracer by calling <c>AddDarkerInstrumentation()</c> on the
/// <c>TracerProviderBuilder</c> (in <c>Paramore.Darker.Extensions.Diagnostics</c>), which wires
/// the source into the OpenTelemetry SDK and adds the tracer to the DI container.
/// When no <see cref="ActivityListener"/> is subscribed the
/// <see cref="ActivitySource.HasListeners"/> guard returns <c>false</c> and every span-creation
/// method returns <c>null</c> — zero allocation overhead when unobserved (NFR2).
/// </remarks>
public sealed class DarkerTracer : IAmADarkerTracer
{
    private readonly TimeProvider _timeProvider;

    /// <inheritdoc />
    public ActivitySource ActivitySource { get; }

    /// <summary>
    /// Initialises a new <see cref="DarkerTracer"/> that owns an
    /// <see cref="System.Diagnostics.ActivitySource"/> named <see cref="DarkerSemanticConventions.SourceName"/>.
    /// </summary>
    /// <param name="timeProvider">
    /// Optional <see cref="TimeProvider"/> seam for deterministic start/end times in tests.
    /// Defaults to <see cref="TimeProvider.System"/> when <c>null</c>.
    /// </param>
    public DarkerTracer(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        var version = typeof(DarkerTracer).Assembly.GetName().Version?.ToString();
        ActivitySource = new ActivitySource(DarkerSemanticConventions.SourceName, version);
    }

    /// <inheritdoc />
    public Activity? CreateQuerySpan<TResult>(IQuery<TResult> query, Activity? parentActivity = null,
        InstrumentationOptions options = InstrumentationOptions.All)
    {
        if (!ActivitySource.HasListeners())
            return null;

        var activity = ActivitySource.StartActivity(
            $"{query.GetType().Name} query",
            ActivityKind.Internal,
            parentActivity?.Id);

        if (activity != null)
            Activity.Current = activity;

        if (activity?.IsAllDataRequested == true && options.HasFlag(InstrumentationOptions.QueryInformation))
        {
            var id = query is Query<TResult> q ? q.Id : null;
            if (id != null)
                activity.SetTag(DarkerSemanticConventions.QueryId, id);
            activity.SetTag(DarkerSemanticConventions.QueryType, query.GetType().FullName);
            activity.SetTag(DarkerSemanticConventions.Operation, "query");
        }

        if (activity?.IsAllDataRequested == true && options.HasFlag(InstrumentationOptions.QueryBody))
            activity.SetTag(DarkerSemanticConventions.QueryBody, SerializeQuery(query));

        return activity;
    }

    /// <inheritdoc />
    public Activity? CreateDbSpan(DbSpanInfo info, Activity? parentActivity,
        InstrumentationOptions options = InstrumentationOptions.All)
    {
        if (!ActivitySource.HasListeners())
            return null;

        // Fuller span creation added in later tasks.
        return null;
    }

    /// <inheritdoc />
    public void AddExceptionToSpan(Activity? span, Exception exception)
    {
        if (span is null) return;
        // Fuller implementation (exception event, Error status, error.type tag) added in later tasks.
    }

    /// <inheritdoc />
    public void EndSpan(Activity? span)
    {
        if (span is null) return;
        // Fuller implementation (Ok status, Stop, restore Activity.Current) added in later tasks.
    }

    // The pipeline closes CreateQuerySpan over IQuery<TResult> so the runtime-type overload is
    // required: the generic Serialize<T>(value, options) overload would serialise the bare
    // IQuery<TResult> interface and emit "{}". value.GetType() restores runtime-type behaviour and
    // composes with a source-generated TypeInfoResolver under AOT.
#if NET8_0_OR_GREATER
    [UnconditionalSuppressMessage(
        "Trimming", "IL2026:RequiresUnreferencedCodeAttribute",
        Justification = "Consumers supply their own JsonSerializerOptions. AOT/trim-safe usage is documented as the consumer responsibility (NFR2). Source-gen TypeInfoResolver is the supported escape hatch.")]
    [UnconditionalSuppressMessage(
        "AOT", "IL3050:RequiresDynamicCodeAttribute",
        Justification = "Same as IL2026 — call site is unavoidable without erasing the public Serialize API. Consumers supply source-gen TypeInfoResolver for full AOT safety.")]
#endif
    private static string SerializeQuery<TResult>(IQuery<TResult> query) =>
        JsonSerializer.Serialize(query, query.GetType(), QueryLoggingJsonOptions.Options);

    /// <summary>Disposes the underlying <see cref="System.Diagnostics.ActivitySource"/>.</summary>
    public void Dispose() => ActivitySource.Dispose();
}
