using Xunit;

namespace Darker.Tests
{
    public class NotReally
    {
        [Fact]
        public void DoesntActuallyWork()
        {
            // todo
            // can't have .net core unit tests for brighter, because polly doesn't support netcoreapp :(
            // see https://github.com/App-vNext/Polly/issues/132
            Assert.True(true);
        }
    }
}
