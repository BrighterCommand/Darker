namespace Paramore.Darker.Testing.Ports
{
    public class TestQueryB : IQuery<int>
    {
        public decimal Number { get; }

        public TestQueryB(decimal number)
        {
            Number = number;
        }
    }
}