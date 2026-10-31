using Paramore.Darker.Policies.Attributes;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler decorated with the retry attribute, whose decorator is an open generic registration.</summary>
    public sealed class OpenGenericDecoratedQueryHandler : QueryHandler<OpenGenericDecoratedQuery, OpenGenericDecoratedResult>
    {
        [RetryableQuery(1)]
        public override OpenGenericDecoratedResult Execute(OpenGenericDecoratedQuery query) => new OpenGenericDecoratedResult();
    }
}
