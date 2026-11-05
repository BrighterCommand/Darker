using System.Collections.Generic;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A sync handler carrying recording decorators at Steps 1, 2 and 3, declared out of order.</summary>
    internal sealed class StepOrderRecordingQueryHandler(List<string> log)
        : QueryHandler<StepOrderRecordingQuery, string>
    {
        [StepOrderRecording(2)]
        [StepOrderRecording(3)]
        [StepOrderRecording(1)]
        public override string Execute(StepOrderRecordingQuery query)
        {
            log.Add("handler");
            return "done";
        }
    }
}
