// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System.Text.Json.Serialization;

namespace Paramore.Darker.Tests.AOT.Scenarios
{
    /// <summary>
    /// Source-generated <see cref="JsonSerializerContext"/> for the AOT harness queries. Installing
    /// <see cref="Default"/> as the <c>QueryLoggingJsonOptions.Options.TypeInfoResolver</c> is the
    /// supported escape hatch (NFR2) that makes the logging decorator's serialization AOT-safe — without
    /// it, native AOT disables reflection-based serialization and the decorator throws.
    /// </summary>
    [JsonSerializable(typeof(AotLoggedQuery))]
    [JsonSerializable(typeof(AotCycleQuery))]
    internal partial class AotTestJsonContext : JsonSerializerContext
    {
    }
}
