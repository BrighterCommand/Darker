using System;
using System.Diagnostics;

namespace Paramore.Darker.Observability;

/// <summary>
/// Role interface for the Darker tracer. Owns the <c>ActivitySource</c> and manages the
/// query-span lifecycle. Implement this interface to provide a custom tracer, or use the
/// default <c>DarkerTracer</c> registered by <c>AddDarkerInstrumentation()</c>.
/// </summary>
public interface IAmADarkerTracer : IDisposable
{
    /// <summary>The <see cref="ActivitySource"/> that creates Darker spans.</summary>
    ActivitySource ActivitySource { get; }

    /// <summary>
    /// Creates a query span for the given query, parented to <paramref name="parentActivity"/>
    /// (or the ambient <c>Activity.Current</c> when null). Returns <c>null</c> when no
    /// <see cref="ActivityListener"/> is registered (zero-overhead path).
    /// When <paramref name="options"/> includes <see cref="InstrumentationOptions.QueryContext"/> and
    /// <paramref name="context"/> is non-null, entries in <see cref="IQueryContext.Bag"/> whose key
    /// begins with <c>spancontext.</c> are copied onto the span as attributes.
    /// </summary>
    Activity? CreateQuerySpan<TResult>(IQuery<TResult> query, Activity? parentActivity = null,
        IQueryContext? context = null, InstrumentationOptions options = InstrumentationOptions.All);

    /// <summary>
    /// Creates a child database span parented to <paramref name="parentActivity"/>.
    /// Returns <c>null</c> when no <see cref="ActivityListener"/> is registered.
    /// </summary>
    Activity? CreateDbSpan(DbSpanInfo info, Activity? parentActivity,
        InstrumentationOptions options = InstrumentationOptions.All);

    /// <summary>
    /// Records <paramref name="exception"/> on <paramref name="span"/>, sets
    /// <c>ActivityStatusCode.Error</c>, and tags <c>error.type</c>. No-op when
    /// <paramref name="span"/> is <c>null</c>.
    /// </summary>
    void AddExceptionToSpan(Activity? span, Exception exception);

    /// <summary>
    /// Sets the span status to <c>Ok</c> (if not already set) and stops the span.
    /// No-op when <paramref name="span"/> is <c>null</c>.
    /// </summary>
    void EndSpan(Activity? span);
}
