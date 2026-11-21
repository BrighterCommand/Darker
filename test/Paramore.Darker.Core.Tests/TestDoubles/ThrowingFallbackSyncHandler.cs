using System;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A sync handler that implements IQueryHandler&lt;,&gt; directly, so its fallback is reached by reflection; both Execute and Fallback throw.</summary>
    internal sealed class ThrowingFallbackSyncHandler : IQueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        public IQueryContext Context { get; set; }

        [ExceptionObserving(1)]
        [FallbackPolicy(2)]
        public SyncTestQuery.Result Execute(SyncTestQuery query)
            => throw new InvalidOperationException("trigger fallback");

        public SyncTestQuery.Result Fallback(SyncTestQuery query)
            => throw new NotSupportedException("fallback failed");
    }
}
