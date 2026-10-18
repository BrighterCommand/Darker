namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class ThrowingScopeResult
    {
        public ThrowingScopeResult(string value) => Value = value;

        public string Value { get; }
    }
}
