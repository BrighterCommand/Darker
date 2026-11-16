using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>An async handler whose ExecuteAsync throws under [FallbackPolicyAttributeAsync] and whose FallbackAsync is implemented only explicitly, so it has no public FallbackAsync.</summary>
    internal sealed class ExplicitFallbackAsyncHandler : IQueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        public IQueryContext Context { get; set; }

        [FallbackPolicyAttributeAsync(1)]
        public Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("trigger fallback");

        Task<AsyncTestQuery.Result> IQueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>.FallbackAsync(
            AsyncTestQuery query, CancellationToken cancellationToken)
            => Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
    }
}
