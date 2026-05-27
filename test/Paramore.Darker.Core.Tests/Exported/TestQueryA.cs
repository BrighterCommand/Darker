using System;

namespace Paramore.Darker.Core.Tests.Exported
{
    public class TestQueryA : IQuery<Guid>
    {
        public Guid Id { get; }

        public TestQueryA(Guid id)
        {
            Id = id;
        }
    }
}