namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class FaultingAsyncDisposeStreamItem
    {
        public FaultingAsyncDisposeStreamItem(string value) => Value = value;

        public string Value { get; }
    }
}
