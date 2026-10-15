namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// Has the ambient scope disposed in its constructor, so the next component the pipeline builds
    /// from that scope (its decorator) meets a disposed scope.
    /// </summary>
    public sealed class ScopeDisposingHandler : QueryHandler<ScopeDisposingQuery, SharedRecordingResult>
    {
        public ScopeDisposingHandler(AmbientScopeOwner owner) => owner.DisposeAmbientScope();

        [InstanceRecordingDecorator(1)]
        public override SharedRecordingResult Execute(ScopeDisposingQuery query) => new SharedRecordingResult();
    }
}
