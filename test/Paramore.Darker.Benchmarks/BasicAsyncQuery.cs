using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Benchmarks
{
    public class BasicAsyncQuery : IQuery<bool>
    {
    }

    public class BasicAsyncQueryHandler : AsyncQueryHandler<BasicAsyncQuery, bool>
    {
        public override Task<bool> ExecuteAsync(BasicAsyncQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(true);
        }
    }
}