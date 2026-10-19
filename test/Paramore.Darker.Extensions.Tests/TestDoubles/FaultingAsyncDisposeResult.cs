namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class FaultingAsyncDisposeResult
    {
        public FaultingAsyncDisposeResult(string value) => Value = value;

        public string Value { get; }
    }
}
