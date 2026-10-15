using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class SecondSharedResultQueryHandler : QueryHandler<SecondSharedResultQuery, SharedRecordingResult>
    {
        private readonly Guid _id = Guid.NewGuid();
        private readonly ITrackedDependency _dependency;

        public SecondSharedResultQueryHandler(ITrackedDependency dependency) => _dependency = dependency;

        [InstanceRecordingDecorator(1)]
        public override SharedRecordingResult Execute(SecondSharedResultQuery query)
            => new SharedRecordingResult { HandlerId = _id, HandlerDependency = _dependency };
    }
}
