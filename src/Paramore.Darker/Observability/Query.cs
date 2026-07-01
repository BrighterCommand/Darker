using System;

namespace Paramore.Darker.Observability;

/// <summary>
/// Optional base class for queries that require a stable, per-instance identity.
/// Derives from <see cref="IQuery{TResult}"/> and defaults <see cref="Id"/> to a new
/// <see cref="Guid"/> string so callers need not supply one explicitly.
/// </summary>
/// <remarks>
/// Implementing <see cref="IQuery{TResult}"/> directly remains fully supported and carries no
/// <c>Id</c>; derive from this class only when query identity is needed (e.g. for distributed
/// tracing via <c>DarkerTracer</c>).
/// </remarks>
/// <typeparam name="TResult">The type returned by the query handler.</typeparam>
public abstract class Query<TResult> : IQuery<TResult>
{
    /// <summary>Initialises a new query with a defaulted-GUID <see cref="Id"/>.</summary>
    protected Query() { }

    /// <summary>Initialises a new query with the supplied <paramref name="id"/>.</summary>
    /// <param name="id">A caller-supplied identifier for this query instance.</param>
    protected Query(string id) => Id = id;

    /// <summary>
    /// A string identifier that is unique per instance.  Defaults to a new <see cref="Guid"/>
    /// formatted as a lowercase hyphenated string (e.g. <c>"3f2504e0-4f89-11d3-9a0c-0305e82c3301"</c>).
    /// Can be overridden at construction time via the <c>init</c> accessor or by passing a value
    /// to <see cref="Query{TResult}(string)"/>.
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();
}
