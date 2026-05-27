using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class SyncTestQuery : IQuery<SyncTestQuery.Result>
    {
        public Guid Id { get; }

        public SyncTestQuery(Guid id)
        {
            Id = id;
        }

        internal class Result
        {
            public Guid Value { get; set; }
        }
    }
}
