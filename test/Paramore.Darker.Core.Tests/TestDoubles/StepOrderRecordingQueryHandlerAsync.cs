using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>An async handler carrying recording decorators at Steps 1, 2 and 3, declared out of order.</summary>
    internal sealed class StepOrderRecordingQueryHandlerAsync(List<string> log)
        : QueryHandlerAsync<StepOrderRecordingAsyncQuery, string>
    {
        [StepOrderRecordingAsync(2)]
        [StepOrderRecordingAsync(3)]
        [StepOrderRecordingAsync(1)]
        public override Task<string> ExecuteAsync(StepOrderRecordingAsyncQuery query,
            CancellationToken cancellationToken = default)
        {
            log.Add("handler");
            return Task.FromResult("done");
        }
    }
}
