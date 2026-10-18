namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class AsyncLifetimeFaultResult
    {
        public AsyncLifetimeFaultResult(string value) => Value = value;

        public string Value { get; }
    }
}
