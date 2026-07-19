using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class DatedStreamQuery : IStreamQuery<string>
    {
        public DateTime Date { get; }

        public DatedStreamQuery(DateTime date)
        {
            Date = date;
        }
    }

    internal sealed class LegacyDatedStreamHandler : IStreamQueryHandler<DatedStreamQuery, string>
    {
        public IQueryContext Context { get; set; }

        public async IAsyncEnumerable<string> ExecuteAsync(
            DatedStreamQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "legacy";
            await System.Threading.Tasks.Task.CompletedTask;
        }
    }

    internal sealed class NewDatedStreamHandler : IStreamQueryHandler<DatedStreamQuery, string>
    {
        public IQueryContext Context { get; set; }

        public async IAsyncEnumerable<string> ExecuteAsync(
            DatedStreamQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "new";
            await System.Threading.Tasks.Task.CompletedTask;
        }
    }
}
