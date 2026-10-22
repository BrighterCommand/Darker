namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class LoggingFaultAsyncResult<TTag>
    {
        public LoggingFaultAsyncResult(string value) => Value = value;

        public string Value { get; }
    }
}
