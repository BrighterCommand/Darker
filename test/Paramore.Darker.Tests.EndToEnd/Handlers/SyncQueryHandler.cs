// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

namespace Paramore.Darker.Tests.EndToEnd.Handlers
{
    /// <summary>Handles <see cref="SyncQuery"/> by returning the fixed value <c>"sync"</c>.</summary>
    public sealed class SyncQueryHandler : QueryHandler<SyncQuery, string>
    {
        /// <inheritdoc />
        public override string Execute(SyncQuery query) => "sync";
    }
}
