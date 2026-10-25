using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>An async handler whose ExecuteAsync carries two decorator attributes at the same Step.</summary>
    internal class DuplicateStepAsyncHandler : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        [PassThroughAsync(2)]
        [FallbackPolicyAttributeAsync(2)]
        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
        }
    }
}
