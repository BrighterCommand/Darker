using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class AsyncHandlerWithFallback : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        [FallbackPolicyAttributeAsync(1)]
        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
        {
            Context.Bag.Add("executed", true);
            throw new InvalidOperationException("trigger fallback");
        }

        public override Task<AsyncTestQuery.Result> FallbackAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
        {
            Context.Bag.Add("fell-back", true);
            return Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
        }
    }
}
