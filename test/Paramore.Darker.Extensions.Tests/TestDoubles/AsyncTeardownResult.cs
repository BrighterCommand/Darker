namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class AsyncTeardownResult
    {
        public AsyncTeardownResult(string value) => Value = value;

        public string Value { get; }
    }
}
