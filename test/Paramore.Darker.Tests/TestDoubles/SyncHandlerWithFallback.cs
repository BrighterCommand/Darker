using System;
using Paramore.Darker.Attributes;

namespace Paramore.Darker.Tests.TestDoubles
{
    public class SyncHandlerWithFallback : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        [FallbackPolicy(1)]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
        {
            Context.Bag.Add("executed", true);
            throw new InvalidOperationException("trigger fallback");
        }

        public override SyncTestQuery.Result Fallback(SyncTestQuery query)
        {
            Context.Bag.Add("fell-back", true);
            return new SyncTestQuery.Result { Value = query.Id };
        }
    }
}
