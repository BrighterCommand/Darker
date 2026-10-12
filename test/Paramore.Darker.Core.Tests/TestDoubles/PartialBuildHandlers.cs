using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    // Each handler carries a pass-through decorator at step 2 (created first) and a decorator whose
    // initialisation throws at step 1 (created second).

    internal sealed class PartialBuildQuery : IQuery<string>
    {
    }

    internal sealed class PartialBuildHandler : QueryHandler<PartialBuildQuery, string>
    {
        [FirstPassThrough(step: 2)]
        [ThrowingInit(step: 1)]
        public override string Execute(PartialBuildQuery query) => "never-reached";
    }

    internal sealed class PartialBuildAsyncQuery : IQuery<string>
    {
    }

    internal sealed class PartialBuildAsyncHandler : QueryHandlerAsync<PartialBuildAsyncQuery, string>
    {
        [PassThroughAsync(step: 2)]
        [ThrowingInitAsync(step: 1)]
        public override Task<string> ExecuteAsync(PartialBuildAsyncQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult("never-reached");
    }

    internal sealed class PartialBuildStreamQuery : IStreamQuery<string>
    {
    }

    internal sealed class PartialBuildStreamHandler : IStreamQueryHandler<PartialBuildStreamQuery, string>
    {
        public IQueryContext Context { get; set; }

        [PassThroughStream(step: 2)]
        [ThrowingInitStream(step: 1)]
        public async IAsyncEnumerable<string> ExecuteAsync(
            PartialBuildStreamQuery query,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            yield return "never-reached";
            await Task.CompletedTask;
        }
    }
}
