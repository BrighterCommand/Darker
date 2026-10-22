namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class AsyncTeardownStreamItem
    {
        public AsyncTeardownStreamItem(string value) => Value = value;

        public string Value { get; }
    }
}
