#region Licence
/* The MIT License (MIT)
Copyright © 2026 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Paramore.Darker.Exceptions;

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
        {
            var key = c.CacheKey;
            if (string.IsNullOrWhiteSpace(key))
                throw new ConfigurationException(
                    $"Query '{query.GetType().FullName}' implements IAmCacheable but its CacheKey returned a null, empty, or whitespace value. Provide a non-empty CacheKey.");
            return key;
        }

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
            WriteProperty(writer, prop, query);

        writer.WriteEndObject();
        writer.Flush();

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static void WriteProperty(Utf8JsonWriter writer, PropertyInfo prop, object query)
    {
        writer.WritePropertyName(prop.Name);

        var value = prop.GetValue(query);
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        // Serialize the value using its runtime type rather than the declared property type.
        // Serializing against the declared type drops the distinguishing data of anything held
        // behind an interface or abstract base (System.Text.Json emits only the declared type's
        // members — often "{}" for a marker interface), collapsing distinct values onto the same
        // cache key. The runtime type serializes the concrete value in full while still writing
        // boxed value types (int, bool, etc.) as JSON numbers/booleans, not objects.
        // System.Text.Json always uses invariant-culture for number formatting.
        JsonSerializer.Serialize(writer, value, value.GetType());
    }
}
