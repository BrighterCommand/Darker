// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Tests.EndToEnd.Handlers
{
    /// <summary>Handles <see cref="ScopedDependencyQuery"/> by reporting the Scoped dependency it received.</summary>
    public sealed class ScopedDependencyQueryHandler : QueryHandlerAsync<ScopedDependencyQuery, ScopedDependencyObservation>
    {
        private readonly ScopedDependency _dependency;

        /// <summary>Initializes a new instance of the <see cref="ScopedDependencyQueryHandler"/> class.</summary>
        /// <param name="dependency">The Scoped dependency.</param>
        public ScopedDependencyQueryHandler(ScopedDependency dependency)
        {
            _dependency = dependency;
        }

        /// <inheritdoc />
        public override Task<ScopedDependencyObservation> ExecuteAsync(ScopedDependencyQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ScopedDependencyObservation(_dependency, _dependency.IsDisposed));
        }
    }
}
