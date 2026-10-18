namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class StreamLifetimeFaultItem
    {
        public StreamLifetimeFaultItem(string value) => Value = value;

        public string Value { get; }
    }
}
