using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class FirstSharedResultQueryHandler : QueryHandler<FirstSharedResultQuery, SharedRecordingResult>
    {
        private readonly Guid _id = Guid.NewGuid();
        private readonly ITrackedDependency _dependency;

        public FirstSharedResultQueryHandler(ITrackedDependency dependency) => _dependency = dependency;

        [InstanceRecordingDecorator(1)]
        public override SharedRecordingResult Execute(FirstSharedResultQuery query)
            => new SharedRecordingResult { HandlerId = _id, HandlerDependency = _dependency };
    }
}
