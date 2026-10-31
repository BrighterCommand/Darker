namespace Paramore.Darker.Extensions.Tests.TestDoubles.Other
{
    /// <summary>
    /// A scope provider with the same short name as <see cref="Paramore.Darker.Extensions.Tests.TestDoubles.TestScopeProvider"/>
    /// but a different namespace, so the two are different types that render the same short name.
    /// </summary>
    public sealed class TestScopeProvider : IAmAScopeProvider
    {
        public IAmAScope GetAmbient(ScopeAffinity affinity) => null;
    }
}
