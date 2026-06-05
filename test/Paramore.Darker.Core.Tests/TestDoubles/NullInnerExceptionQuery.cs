using System.Reflection;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class NullInnerExceptionQuery : IQuery<string> { }

    internal class NullInnerExceptionQueryHandler : QueryHandler<NullInnerExceptionQuery, string>
    {
        public override string Execute(NullInnerExceptionQuery query)
        {
            throw new TargetInvocationException(null);
        }
    }
}
