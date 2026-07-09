using System.Collections.Generic;
using System.Threading;

namespace Paramore.Darker.Core.Tests.Exported
{
    public class ExportedStreamQueryHandler : IStreamQueryHandler<ExportedStreamQuery, string>
    {
        public IQueryContext Context { get; set; }

#pragma warning disable CS1998
        public async IAsyncEnumerable<string> ExecuteAsync(ExportedStreamQuery query, CancellationToken cancellationToken = default)
#pragma warning restore CS1998
        {
            yield return "exported-item";
        }
    }
}
