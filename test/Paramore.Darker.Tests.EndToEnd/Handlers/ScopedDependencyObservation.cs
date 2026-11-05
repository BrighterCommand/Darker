// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

namespace Paramore.Darker.Tests.EndToEnd.Handlers
{
    /// <summary>What <see cref="ScopedDependencyQueryHandler"/> saw of its Scoped dependency while it ran.</summary>
    /// <param name="Dependency">The dependency instance the handler received.</param>
    /// <param name="WasDisposedDuringQuery">Whether the dependency was already disposed while the handler ran.</param>
    public sealed record ScopedDependencyObservation(ScopedDependency? Dependency, bool WasDisposedDuringQuery);
}
