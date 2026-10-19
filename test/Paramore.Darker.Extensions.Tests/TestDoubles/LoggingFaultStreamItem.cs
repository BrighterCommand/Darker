namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class LoggingFaultStreamItem<TTag>
    {
        public LoggingFaultStreamItem(string value) => Value = value;

        public string Value { get; }
    }
}
