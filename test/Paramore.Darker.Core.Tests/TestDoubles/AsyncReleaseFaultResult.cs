namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class AsyncReleaseFaultResult<TTag>
    {
        public AsyncReleaseFaultResult(string value) => Value = value;

        public string Value { get; }
    }
}
