using System;

namespace Paramore.Darker.Tests.TestDoubles
{
    public class AsyncTestQuery : IQuery<AsyncTestQuery.Result>
    {
        public Guid Id { get; }

        public AsyncTestQuery(Guid id)
        {
            Id = id;
        }

        public class Result
        {
            public Guid Value { get; set; }
        }
    }
}
