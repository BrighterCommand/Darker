using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    // Each path has result types of its own, so the static per-TResult logger in the pipeline
    // is created against the logger factory of the test that first runs it.

    public sealed class AsyncTeardownQuery : IQuery<AsyncTeardownResult>
    {
    }

    public sealed class AsyncTeardownResult
    {
        public AsyncTeardownResult(string value) => Value = value;

        public string Value { get; }
    }

    public sealed class AsyncTeardownQueryHandler : QueryHandlerAsync<AsyncTeardownQuery, AsyncTeardownResult>
    {
        public const string RESULT = "async-teardown";

        private readonly YieldingAsyncOnlyDisposableDependency _dependency;

        public AsyncTeardownQueryHandler(YieldingAsyncOnlyDisposableDependency dependency) => _dependency = dependency;

        public override Task<AsyncTeardownResult> ExecuteAsync(AsyncTeardownQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(new AsyncTeardownResult(_dependency != null ? RESULT : null));
    }

    public sealed class AsyncTeardownStreamQuery : IStreamQuery<AsyncTeardownStreamItem>
    {
    }

    public sealed class AsyncTeardownStreamItem
    {
        public AsyncTeardownStreamItem(string value) => Value = value;

        public string Value { get; }
    }

    public sealed class AsyncTeardownStreamQueryHandler : StreamQueryHandler<AsyncTeardownStreamQuery, AsyncTeardownStreamItem>
    {
        public const string ITEM = "async-teardown-item";

        private readonly YieldingAsyncOnlyDisposableDependency _dependency;

        public AsyncTeardownStreamQueryHandler(YieldingAsyncOnlyDisposableDependency dependency) => _dependency = dependency;

        public override async IAsyncEnumerable<AsyncTeardownStreamItem> ExecuteAsync(
            AsyncTeardownStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            yield return new AsyncTeardownStreamItem(_dependency != null ? ITEM : null);
        }
    }
}
