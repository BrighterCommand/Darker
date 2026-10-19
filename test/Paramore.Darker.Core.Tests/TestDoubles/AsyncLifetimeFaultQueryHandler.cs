using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class AsyncLifetimeFaultQueryHandler : QueryHandlerAsync<AsyncLifetimeFaultQuery, AsyncLifetimeFaultResult>
    {
        public const string RESULT = "async-lifetime-fault";

        public override Task<AsyncLifetimeFaultResult> ExecuteAsync(AsyncLifetimeFaultQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(new AsyncLifetimeFaultResult(RESULT));
    }
}
