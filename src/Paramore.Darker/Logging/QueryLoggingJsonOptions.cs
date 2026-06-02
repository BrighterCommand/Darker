using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Paramore.Darker.Logging
{
    /// <summary>
    /// Holds the process-global <see cref="JsonSerializerOptions"/> used by the query logging
    /// decorators to serialise the <c>{Query}</c> log argument. Mirrors the shape of Brighter's
    /// <c>JsonSerialisationOptions</c>.
    /// </summary>
    /// <remarks>
    /// The default instance configures <see cref="ReferenceHandler.IgnoreCycles"/> so that
    /// EF Core-backed query objects with navigation-property cycles do not throw on the logging
    /// hot path. Intended to be configured once at application startup, before any query is handled.
    /// </remarks>
    public static class QueryLoggingJsonOptions
    {
        private static JsonSerializerOptions _options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        /// <summary>
        /// Gets or sets the <see cref="JsonSerializerOptions"/> used when serialising the query body
        /// for logging. Defaults to a new instance with <see cref="ReferenceHandler.IgnoreCycles"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when assigned <c>null</c>.</exception>
        public static JsonSerializerOptions Options
        {
            get => _options;
            set => _options = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}
