namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal sealed class EveryReleaseThrowsResult
    {
        public EveryReleaseThrowsResult(string value) => Value = value;

        public string Value { get; }
    }
}
