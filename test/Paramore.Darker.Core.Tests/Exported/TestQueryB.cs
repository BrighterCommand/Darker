namespace Paramore.Darker.Core.Tests.Exported
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