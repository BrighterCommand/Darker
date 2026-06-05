using System;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class TestQueryHandlerWithCatchAllFallback : QueryHandler<TestQuery, TestQuery.Result>
    {
        [FallbackPolicy(1)]
        public override TestQuery.Result Execute(TestQuery query)
        {
            Context.Bag.Add("Check1", true);
            throw new FormatException();
        }

        public override TestQuery.Result Fallback(TestQuery query)
        {
            Context.Bag.Add("Check2", true);
            return new TestQuery.Result();
        }
    }
}
