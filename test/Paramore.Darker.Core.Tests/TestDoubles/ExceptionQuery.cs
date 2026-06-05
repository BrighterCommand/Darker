using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class ExceptionQuery : IQuery<ExceptionQuery.Result>
    {
        public class Result { }
    }

    internal class ExceptionQueryHandler : QueryHandler<ExceptionQuery, ExceptionQuery.Result>
    {
        public override ExceptionQuery.Result Execute(ExceptionQuery query)
        {
            throw new InvalidOperationException("Test exception from Execute");
        }
    }
}
