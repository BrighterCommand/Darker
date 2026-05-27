using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Tests.TestDoubles
{
    /// <summary>
    /// An async handler that incorrectly uses a sync attribute on ExecuteAsync.
    /// This should cause a ConfigurationException at pipeline build time.
    /// </summary>
    internal class AsyncHandlerWithSyncAttribute : QueryHandlerAsync<AsyncTestQuery, AsyncTestQuery.Result>
    {
        [FallbackPolicy(1)]
        public override Task<AsyncTestQuery.Result> ExecuteAsync(AsyncTestQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AsyncTestQuery.Result { Value = query.Id });
        }
    }
}
