using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class LegacyDatedQueryHandlerAsync : QueryHandlerAsync<DatedQuery, string>
    {
        public override Task<string> ExecuteAsync(DatedQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult("legacy");
    }
}
