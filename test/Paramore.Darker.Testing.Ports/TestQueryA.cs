using System;

namespace Paramore.Darker.Testing.Ports
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