using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class ThrowingScopeAsyncQueryHandler : QueryHandlerAsync<ThrowingScopeAsyncQuery, ThrowingScopeAsyncResult>
    {
        public const string RESULT = "throwing-scope-async";

        public override Task<ThrowingScopeAsyncResult> ExecuteAsync(ThrowingScopeAsyncQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(new ThrowingScopeAsyncResult(RESULT));
    }
}
