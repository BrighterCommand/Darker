namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class DisposalRecordingStreamItem
    {
        public DisposalRecordingStreamItem(string value) => Value = value;

        public string Value { get; }
    }
}
