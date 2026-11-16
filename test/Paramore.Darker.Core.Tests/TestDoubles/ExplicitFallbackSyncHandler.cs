using System;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A sync handler whose Execute throws under [FallbackPolicy] and whose Fallback is implemented only explicitly, so it has no public Fallback.</summary>
    internal sealed class ExplicitFallbackSyncHandler : IQueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        public IQueryContext Context { get; set; }

        [FallbackPolicy(1)]
        public SyncTestQuery.Result Execute(SyncTestQuery query)
            => throw new InvalidOperationException("trigger fallback");

        SyncTestQuery.Result IQueryHandler<SyncTestQuery, SyncTestQuery.Result>.Fallback(SyncTestQuery query)
            => new SyncTestQuery.Result { Value = query.Id };
    }
}
