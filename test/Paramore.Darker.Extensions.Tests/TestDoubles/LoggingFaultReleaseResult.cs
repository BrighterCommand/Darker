namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class LoggingFaultReleaseResult
    {
        public LoggingFaultReleaseResult(string value) => Value = value;

        public string Value { get; }
    }
}
