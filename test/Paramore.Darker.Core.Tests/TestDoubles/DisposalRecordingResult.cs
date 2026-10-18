namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class DisposalRecordingResult
    {
        public DisposalRecordingResult(string value) => Value = value;

        public string Value { get; }
    }
}
