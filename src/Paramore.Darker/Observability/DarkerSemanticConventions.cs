using System.Collections.Generic;
#if NET8_0_OR_GREATER
using System.Collections.Frozen;
#endif

namespace Paramore.Darker.Observability;

/// <summary>
/// String constants for all Darker attribute and event key names used in distributed tracing.
/// Using named constants rather than inline string literals ensures that a typo cannot silently
/// break tracing by emitting an attribute no collector is listening for.
/// </summary>
/// <remarks>
/// Attribute names follow the Brighter semantic-convention style and are prefixed with
/// <c>paramore.darker.</c>. Database span attributes follow the OpenTelemetry database-spans
/// semantic conventions (<c>db.*</c>).
/// </remarks>
public static class DarkerSemanticConventions
{
    // ── ActivitySource ────────────────────────────────────────────────────────

    /// <summary>The name of the <see cref="System.Diagnostics.ActivitySource"/> used by Darker.</summary>
    public const string SourceName = "paramore.darker";

    // ── Query span attributes ─────────────────────────────────────────────────

    /// <summary>The unique identifier of the query (<c>paramore.darker.queryid</c>).</summary>
    public const string QueryId = "paramore.darker.queryid";

    /// <summary>The full CLR type name of the query (<c>paramore.darker.querytype</c>).</summary>
    public const string QueryType = "paramore.darker.querytype";

    /// <summary>The Darker processor operation name, always <c>query</c> (<c>paramore.darker.operation</c>).</summary>
    public const string Operation = "paramore.darker.operation";

    /// <summary>The query serialised as JSON, emitted only when <c>QueryBody</c> instrumentation is enabled (<c>paramore.darker.query_body</c>).</summary>
    public const string QueryBody = "paramore.darker.query_body";

    // ── SpanContext bag prefix ────────────────────────────────────────────────

    /// <summary>
    /// Prefix used to identify <see cref="IQueryContext.Bag"/> entries that should be
    /// promoted onto the query span as attributes when <c>QueryContext</c> instrumentation
    /// is enabled (<c>spancontext.</c>).
    /// </summary>
    public const string SpanContextPrefix = "spancontext.";

    // ── Pipeline step event attributes ───────────────────────────────────────

    /// <summary>The full CLR type name of the handler or decorator step (<c>paramore.darker.handlername</c>).</summary>
    public const string HandlerName = "paramore.darker.handlername";

    /// <summary>Whether the step executes synchronously or asynchronously (<c>paramore.darker.handlertype</c>).</summary>
    public const string HandlerType = "paramore.darker.handlertype";

    /// <summary>
    /// Boolean attribute set to <c>true</c> on the terminal handler (the sink) event,
    /// distinguishing it from decorator events (<c>paramore.darker.is_sink</c>).
    /// </summary>
    public const string IsSink = "paramore.darker.is_sink";

    // ── Exception / error ─────────────────────────────────────────────────────

    /// <summary>The exception type name, following the OpenTelemetry exception convention (<c>error.type</c>).</summary>
    public const string ErrorType = "error.type";

    // ── Database span attributes (OTel db.* semantic conventions) ────────────

    /// <summary>The database system identifier, e.g. <c>mssql</c>, <c>postgresql</c> (<c>db.system</c>).</summary>
    public const string DbSystem = "db.system";

    /// <summary>The name of the database being accessed (<c>db.name</c>).</summary>
    public const string DbName = "db.name";

    /// <summary>The database operation name, e.g. <c>SELECT</c>, <c>INSERT</c> (<c>db.operation</c>).</summary>
    public const string DbOperation = "db.operation";

    /// <summary>The name of the collection or table for document/key-value stores (<c>db.collection.name</c>).</summary>
    public const string DbCollectionName = "db.collection.name";

    /// <summary>The SQL table name for relational databases (<c>db.sql.table</c>).</summary>
    public const string DbSqlTable = "db.sql.table";

    /// <summary>The database server host address (<c>server.address</c>).</summary>
    public const string ServerAddress = "server.address";

    /// <summary>The database statement (query/command) text (<c>db.statement</c>).</summary>
    public const string DbStatement = "db.statement";

    /// <summary>The database user name (<c>db.user</c>).</summary>
    public const string DbUser = "db.user";

    // ── Cache span attributes ─────────────────────────────────────────────────

    /// <summary>
    /// The cache-lookup outcome attribute key (<c>paramore.darker.cache.outcome</c>).
    /// Value is <c>"hit"</c> when a cached result is returned, or <c>"miss"</c> when the handler is invoked.
    /// </summary>
    public const string CacheOutcome = "paramore.darker.cache.outcome";

    // ── Meter / metric names ──────────────────────────────────────────────────

    /// <summary>The name of the <see cref="System.Diagnostics.Metrics.Meter"/> used by Darker.</summary>
    public const string MeterName = "paramore.darker";

    /// <summary>The name of the query-duration histogram instrument (<c>paramore.darker.query.duration</c>).</summary>
    public const string QueryDurationMetricName = "paramore.darker.query.duration";

    /// <summary>The name of the DB-client-operation-duration histogram instrument (<c>db.client.operation.duration</c>).</summary>
    public const string DbClientOperationDurationMetricName = "db.client.operation.duration";

    /// <summary>The name of the cache-requests counter instrument (<c>paramore.darker.cache.requests</c>).</summary>
    public const string CacheRequestsMetricName = "paramore.darker.cache.requests";

    // ── Resource / service attributes ─────────────────────────────────────────

    /// <summary>The service name resource attribute (<c>service.name</c>).</summary>
    public const string ServiceName = "service.name";

    /// <summary>The service version resource attribute (<c>service.version</c>).</summary>
    public const string ServiceVersion = "service.version";

    /// <summary>The service instance ID resource attribute (<c>service.instance.id</c>).</summary>
    public const string ServiceInstanceId = "service.instance.id";

    /// <summary>The service namespace resource attribute (<c>service.namespace</c>).</summary>
    public const string ServiceNamespace = "service.namespace";

    // ── Per-instrument allowed-tag sets ───────────────────────────────────────

    /// <summary>
    /// The low-cardinality tag keys permitted on the <c>paramore.darker.query.duration</c> histogram.
    /// High-cardinality keys such as <see cref="QueryId"/> and <see cref="QueryBody"/> are intentionally excluded.
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly FrozenSet<string> QueryDurationAllowedTags = new[]
#else
    public static readonly HashSet<string> QueryDurationAllowedTags = new()
#endif
    {
        QueryType,
        Operation,
        ErrorType
#if NET8_0_OR_GREATER
    }.ToFrozenSet();
#else
    };
#endif

    /// <summary>
    /// The low-cardinality tag keys permitted on the <c>db.client.operation.duration</c> histogram.
    /// High-cardinality keys such as <see cref="DbStatement"/> and <see cref="DbUser"/> are intentionally excluded.
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly FrozenSet<string> DbClientOperationDurationAllowedTags = new[]
#else
    public static readonly HashSet<string> DbClientOperationDurationAllowedTags = new()
#endif
    {
        DbSystem,
        DbName,
        DbOperation,
        DbSqlTable,
        DbCollectionName,
        ServerAddress,
        ErrorType
#if NET8_0_OR_GREATER
    }.ToFrozenSet();
#else
    };
#endif

    /// <summary>
    /// The low-cardinality tag keys permitted on the <c>paramore.darker.cache.requests</c> counter.
    /// High-cardinality keys such as <see cref="QueryId"/> are intentionally excluded.
    /// </summary>
#if NET8_0_OR_GREATER
    public static readonly FrozenSet<string> CacheRequestsAllowedTags = new[]
#else
    public static readonly HashSet<string> CacheRequestsAllowedTags = new()
#endif
    {
        QueryType,
        CacheOutcome
#if NET8_0_OR_GREATER
    }.ToFrozenSet();
#else
    };
#endif
}
