namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A second scope provider type, registered under a key, so it is distinct from <see cref="TestScopeProvider"/>.
    /// </summary>
    public sealed class KeyedTestScopeProvider : IAmAScopeProvider
    {
        public IAmAScope GetAmbient(ScopeAffinity affinity) => null;
    }
}
