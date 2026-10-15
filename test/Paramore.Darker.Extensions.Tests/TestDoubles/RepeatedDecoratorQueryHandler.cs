using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A handler that applies the same decorator type twice, at different steps.</summary>
    public sealed class RepeatedDecoratorQueryHandler : QueryHandler<RepeatedDecoratorQuery, SharedRecordingResult>
    {
        private readonly Guid _id = Guid.NewGuid();
        private readonly ITrackedDependency _dependency;

        public RepeatedDecoratorQueryHandler(ITrackedDependency dependency) => _dependency = dependency;

        [RepeatableInstanceRecordingDecorator(1)]
        [RepeatableInstanceRecordingDecorator(2)]
        public override SharedRecordingResult Execute(RepeatedDecoratorQuery query)
            => new SharedRecordingResult { HandlerId = _id, HandlerDependency = _dependency };
    }
}
