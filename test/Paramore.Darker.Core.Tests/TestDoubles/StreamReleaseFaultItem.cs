namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class StreamReleaseFaultItem<TTag>
    {
        public StreamReleaseFaultItem(string value) => Value = value;

        public string Value { get; }
    }
}
