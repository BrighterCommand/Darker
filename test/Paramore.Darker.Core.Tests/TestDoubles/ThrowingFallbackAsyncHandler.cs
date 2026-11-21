using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// An async handler that implements IQueryHandlerAsync&lt;,&gt; directly, so its fallback is reached by reflection.
    /// FallbackAsync is not an async method: it throws before returning a Task, which is the shape reflection wraps.
    /// </summary>
    internal sealed class ThrowingFallbackAsyncHandler : IQueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        public IQueryContext Context { get; set; }

        [ExceptionObservingAsync(1)]
        [FallbackPolicyAttributeAsync(2)]
        public Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("trigger fallback");

        public Task<AsyncTestQuery.Result> FallbackAsync(AsyncTestQuery query, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("fallback failed");
    }
}
