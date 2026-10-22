using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
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
