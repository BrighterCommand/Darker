using System;
using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class FallbackExceptionQuery : IQuery<FallbackExceptionQuery.Result>
    {
        public class Result { }
    }

    internal class FallbackExceptionQueryHandler : QueryHandler<FallbackExceptionQuery, FallbackExceptionQuery.Result>
    {
        [FallbackPolicy(step: 1, typeof(InvalidOperationException))]
        public override FallbackExceptionQuery.Result Execute(FallbackExceptionQuery query)
        {
            throw new InvalidOperationException("Test exception from Execute");
        }
        public override FallbackExceptionQuery.Result Fallback(FallbackExceptionQuery query)
        {
            throw new NotSupportedException("Test exception from Fallback");
        }
    }
}
