namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A scope provider that never has an ambient scope. Lifetime rules read only its registration.
    /// </summary>
    public sealed class TestScopeProvider : IAmAScopeProvider
    {
        public IAmAScope GetAmbient(ScopeAffinity affinity) => null;
    }
}
