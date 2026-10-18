namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class DebugLogFaultResult<TTag>(string value)
    {
        public string Value { get; } = value;
    }
}
