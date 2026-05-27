using System;

namespace Paramore.Darker.Core.Tests.Exported
{
    public class TestQueryHandler : IQueryHandler<TestQueryA, Guid>
    {
        public IQueryContext Context { get; set; }

        public Guid Execute(TestQueryA query)
        {
            Context.Bag.Add("id", query.Id);
            return query.Id;
        }

        public Guid Fallback(TestQueryA query)
        {
            throw new NotImplementedException();
        }
    }
}