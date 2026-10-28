namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler whose Execute method has two decorators at the same step.</summary>
    public sealed class DuplicateStepQueryHandler : QueryHandler<DuplicateStepQuery, DuplicateStepResult>
    {
        [RepeatableInstanceRecordingDecorator(1)]
        [RepeatableInstanceRecordingDecorator(1)]
        public override DuplicateStepResult Execute(DuplicateStepQuery query) => new DuplicateStepResult();
    }
}
