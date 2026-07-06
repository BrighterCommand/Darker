using System;

namespace Paramore.Darker.Observability;

/// <summary>
/// Controls which attribute groups are emitted on a query span.
/// Combine values with the bitwise OR operator to enable multiple groups.
/// </summary>
[Flags]
public enum InstrumentationOptions
{
    /// <summary>No attributes emitted.</summary>
    None = 0,

    /// <summary>Query identity, type, and operation attributes, plus step-event tags.</summary>
    QueryInformation = 1,

    /// <summary>The serialised query body (<c>paramore.darker.query_body</c>).</summary>
    QueryBody = 2,

    /// <summary>Span-context bag entries copied from <c>IQueryContext.Bag</c>.</summary>
    QueryContext = 4,

    /// <summary><c>db.*</c> attributes on database child spans.</summary>
    DatabaseInformation = 8,

    /// <summary>All attribute groups (the union of every individual flag).</summary>
    All = QueryInformation | QueryBody | QueryContext | DatabaseInformation
}
