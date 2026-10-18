namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class ThrowingScopeStreamItem
    {
        public ThrowingScopeStreamItem(string value) => Value = value;

        public string Value { get; }
    }
}
