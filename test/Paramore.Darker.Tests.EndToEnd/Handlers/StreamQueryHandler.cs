// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System.Collections.Generic;
using System.Threading;

namespace Paramore.Darker.Tests.EndToEnd.Handlers
{
    /// <summary>Handles <see cref="StreamQuery"/> by yielding 1, 2, 3 in order.</summary>
    public sealed class StreamQueryHandler : StreamQueryHandler<StreamQuery, int>
    {
        /// <inheritdoc />
        public override async IAsyncEnumerable<int> ExecuteAsync(
            StreamQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            for (var i = 1; i <= 3; i++)
            {
                yield return i;
                await System.Threading.Tasks.Task.Yield();
            }
        }
    }
}
