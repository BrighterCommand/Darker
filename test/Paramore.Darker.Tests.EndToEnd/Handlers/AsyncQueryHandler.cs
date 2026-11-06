// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Tests.EndToEnd.Handlers
{
    /// <summary>Handles <see cref="AsyncQuery"/> by returning the fixed value <c>"async"</c>.</summary>
    public sealed class AsyncQueryHandler : QueryHandlerAsync<AsyncQuery, string>
    {
        /// <inheritdoc />
        public override Task<string> ExecuteAsync(AsyncQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult("async");
    }
}
