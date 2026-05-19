using System;

namespace Paramore.Darker.Tests.TestDoubles
{
    public class SyncTestQuery : IQuery<SyncTestQuery.Result>
    {
        public Guid Id { get; }

        public SyncTestQuery(Guid id)
        {
            Id = id;
        }

        public class Result
        {
            public Guid Value { get; set; }
        }
    }
}
