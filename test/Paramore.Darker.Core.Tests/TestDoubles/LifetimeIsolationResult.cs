namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class LifetimeIsolationResult
    {
        public LifetimeIsolationResult(string value) => Value = value;

        public string Value { get; }
    }
}
