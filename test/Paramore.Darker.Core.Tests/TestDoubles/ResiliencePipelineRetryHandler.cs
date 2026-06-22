using System;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A sync handler decorated with <see cref="UseResiliencePipelineAttribute"/> that fails
    /// transiently (the first two invocations throw) so a retry pipeline drives it to success.
    /// Used for end-to-end builder/DI wiring tests.
    /// </summary>
    internal class ResiliencePipelineRetryHandler : QueryHandler<SyncTestQuery, SyncTestQuery.Result>
    {
        private int _calls;

        [UseResiliencePipeline(1, "Retry")]
        public override SyncTestQuery.Result Execute(SyncTestQuery query)
        {
            _calls++;
            if (_calls < 3)
                throw new InvalidOperationException("transient failure");

            return new SyncTestQuery.Result { Value = query.Id };
        }
    }
}
