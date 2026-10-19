namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class ThrowingScopeAsyncResult
    {
        public ThrowingScopeAsyncResult(string value) => Value = value;

        public string Value { get; }
    }
}
