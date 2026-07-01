namespace Paramore.Darker.Observability
{
    /// <summary>
    /// Lists common OTel <c>db.system</c> values for database span classification.
    /// Use <see cref="DbSystemExtensions.ToDbSystemString"/> to obtain the canonical
    /// OTel <c>db.system</c> string for a given value.
    /// </summary>
    public enum DbSystem
    {
        MsSql,
        PostgreSql,
        MySql,
        Sqlite,
        Oracle,
        Db2,
        MongoDb,
        Redis,
        Cassandra,
        Other
    }

    /// <summary>
    /// Extension methods for <see cref="DbSystem"/> that map each value to its canonical
    /// OTel <c>db.system</c> string as defined in the OpenTelemetry semantic conventions.
    /// </summary>
    public static class DbSystemExtensions
    {
        /// <summary>
        /// Returns the canonical OTel <c>db.system</c> string for the given <see cref="DbSystem"/> value.
        /// </summary>
        /// <param name="system">The database system enum value.</param>
        /// <returns>The OTel canonical <c>db.system</c> string.</returns>
        public static string ToDbSystemString(this DbSystem system) => system switch
        {
            DbSystem.MsSql => "mssql",
            DbSystem.PostgreSql => "postgresql",
            DbSystem.MySql => "mysql",
            DbSystem.Sqlite => "sqlite",
            DbSystem.Oracle => "oracle",
            DbSystem.Db2 => "db2",
            DbSystem.MongoDb => "mongodb",
            DbSystem.Redis => "redis",
            DbSystem.Cassandra => "cassandra",
            DbSystem.Other => "other_sql",
            _ => "other_sql"
        };
    }
}
