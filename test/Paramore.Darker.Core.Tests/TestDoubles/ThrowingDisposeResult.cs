namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class ThrowingDisposeResult
    {
        public ThrowingDisposeResult(string value) => Value = value;

        public string Value { get; }
    }
}
