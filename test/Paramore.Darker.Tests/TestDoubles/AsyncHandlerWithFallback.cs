using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Attributes;

namespace Paramore.Darker.Tests.TestDoubles
{
    public class AsyncHandlerWithFallback : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
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
