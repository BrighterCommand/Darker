namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class ThrowingDisposeDependencyResult
    {
        public ThrowingDisposeDependencyResult(string value) => Value = value;

        public string Value { get; }
    }
}
