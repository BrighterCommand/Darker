using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class PipelineScopeAsyncQueryHandler : QueryHandlerAsync<PipelineScopeAsyncQuery, string>
    {
        public const string RESULT = "pipeline-scope-async";

        [PassThroughAsync(step: 1)]
        public override Task<string> ExecuteAsync(PipelineScopeAsyncQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(RESULT);
    }
}
