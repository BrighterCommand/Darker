namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A scope provider whose constructor records that the container built it. It never has an ambient scope.</summary>
    public sealed class ConstructionRecordingScopeProvider : IAmAScopeProvider
    {
        public ConstructionRecordingScopeProvider(ConstructionCounter counter) => counter.ScopeProviderConstructed();

        public IAmAScope GetAmbient(ScopeAffinity affinity) => null;
    }
}
