using Paramore.Darker.Observability;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    internal class QueryWithExplicitId : Query<QueryWithExplicitId.Result>
    {
        internal QueryWithExplicitId(string id) : base(id) { }

        internal class Result { }
    }
}
