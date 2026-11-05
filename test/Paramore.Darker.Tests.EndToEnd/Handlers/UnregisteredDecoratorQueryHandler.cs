// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Logging.Attributes;

namespace Paramore.Darker.Tests.EndToEnd.Handlers
{
    /// <summary>
    /// Handles <see cref="UnregisteredDecoratorQuery"/>. Its logging decorator is never registered, because the
    /// scenario omits <c>AddJsonQueryLogging</c>, so pipeline validation must reject it.
    /// </summary>
    public sealed class UnregisteredDecoratorQueryHandler : QueryHandlerAsync<UnregisteredDecoratorQuery, string>
    {
        /// <inheritdoc />
        [QueryLoggingAttributeAsync(1)]
        public override Task<string> ExecuteAsync(UnregisteredDecoratorQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult("unreachable");
    }
}
