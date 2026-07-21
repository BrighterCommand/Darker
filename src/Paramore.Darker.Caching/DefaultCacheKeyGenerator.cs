using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Paramore.Darker.Caching;

/// <summary>
/// Default cache-key strategy. Uses the query's runtime type (never a generic type parameter)
/// to form the key: <c>"{Type.FullName}|{orderedInvariantJson}"</c>.
/// <para>
/// The JSON body is deterministic across runs: public readable properties are serialised in
/// ordinal name order using <c>System.Text.Json</c> (which always uses invariant-culture number
/// formatting), with explicit <c>null</c> values and no whitespace.
/// </para>
/// <para>
/// Queries that implement <see cref="IAmCacheable"/> use their own <see cref="IAmCacheable.CacheKey"/>
/// instead; this check is applied before the default type+JSON strategy.
/// </para>
/// </summary>
public sealed class DefaultCacheKeyGenerator : ICacheKeyGenerator
{
    /// <inheritdoc />
    public string GenerateKey(object query)
    {
        if (query is IAmCacheable c)
            return c.CacheKey;

        var type = query.GetType();
        return $"{type.FullName}|{BuildOrderedJson(query, type)}";
    }

    private static string BuildOrderedJson(object query, Type type)
    {
        // Reflect on the runtime type — never typeof(TQuery).
        // Order properties by name with ordinal comparison for stable, deterministic output.
        var properties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .OrderBy(p => p.Name, StringComparer.Ordinal);

        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms);

        writer.WriteStartObject();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(query);
            writer.WritePropertyName(prop.Name);

            if (value is null)
            {
                writer.WriteNullValue();
            }
            else
            {
                // Serialize the value using the declared property type so that boxed value
                // types (int, bool, etc.) are written as JSON numbers/booleans, not objects.
                // System.Text.Json always uses invariant-culture for number formatting.
                JsonSerializer.Serialize(writer, value, prop.PropertyType);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(ms.ToArray());
    }
}
