using System.Collections.Generic;

namespace Paramore.Darker.Observability
{
    /// <summary>
    /// Carries the attributes needed to shape a database span.
    /// Required members are the database system, name, and operation.
    /// Optional members cover the table, server address, statement, user, and a free attribute bag.
    /// Mirrors Brighter's <c>BoxSpanInfo</c> for ecosystem consistency (ADR 0017 §8).
    /// </summary>
    public record DbSpanInfo(DbSystem DbSystem, string DbName, string DbOperation, string? DbTable = null)
    {
        /// <summary>The host name or IP address of the database server.</summary>
        public string? ServerAddress { get; init; }

        /// <summary>The database statement (e.g. SQL) executed during the span.</summary>
        public string? DbStatement { get; init; }

        /// <summary>The username used to access the database.</summary>
        public string? DbUser { get; init; }

        /// <summary>Additional free-form span attributes expressed as key-value pairs.</summary>
        public IDictionary<string, string>? DbAttributes { get; init; }
    }
}
