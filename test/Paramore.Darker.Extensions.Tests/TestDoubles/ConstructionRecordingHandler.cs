namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A decorated handler whose constructor records that the container built it.</summary>
    public sealed class ConstructionRecordingHandler : QueryHandler<ConstructionRecordingQuery, ConstructionRecordingResult>
    {
        public ConstructionRecordingHandler(ConstructionCounter counter) => counter.HandlerConstructed();

        [ConstructionRecordingDecorator(1)]
        public override ConstructionRecordingResult Execute(ConstructionRecordingQuery query) => new ConstructionRecordingResult();
    }
}
