using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class AsyncTestQuery : IQuery<AsyncTestQuery.Result>
    {
        public Guid Id { get; }

        public AsyncTestQuery(Guid id)
        {
            Id = id;
        }

        internal class Result
        {
            public Guid Value { get; set; }
        }
    }
}
