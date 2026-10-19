using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A handler that implements the sync handler interface, so it can be registered in the sync registry,
    /// and also exposes <c>ExecuteAsync</c>, so the async pipeline can run it. It carries one async decorator.
    /// </summary>
    internal sealed class DualPipelineScopeQueryHandler : IQueryHandler<PipelineScopeAsyncQuery, string>
    {
        public const string RESULT = "dual-pipeline-scope";

        public IQueryContext Context { get; set; }

        public string Execute(PipelineScopeAsyncQuery query) => throw new NotSupportedException("This handler is run through ExecuteAsync.");

        public string Fallback(PipelineScopeAsyncQuery query) => throw new NotSupportedException();

        [PassThroughAsync(step: 1)]
        public Task<string> ExecuteAsync(PipelineScopeAsyncQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(RESULT);

        public Task<string> FallbackAsync(PipelineScopeAsyncQuery query, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
