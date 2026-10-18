namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class AsyncOnlyDisposableResult
    {
        public AsyncOnlyDisposableResult(string value) => Value = value;

        public string Value { get; }
    }
}
