using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class PartialBuildAsyncHandler : QueryHandlerAsync<PartialBuildAsyncQuery, string>
    {
        [PassThroughAsync(step: 2)]
        [ThrowingInitAsync(step: 1)]
        public override Task<string> ExecuteAsync(PartialBuildAsyncQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult("never-reached");
    }
}
