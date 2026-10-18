namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class LoggingFaultLifetimeResult
    {
        public LoggingFaultLifetimeResult(string value) => Value = value;

        public string Value { get; }
    }
}
