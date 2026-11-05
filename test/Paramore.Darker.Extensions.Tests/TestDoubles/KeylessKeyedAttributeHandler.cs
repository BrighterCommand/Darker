using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A handler whose constructor parameter carries <c>[FromKeyedServices]</c> with no key.</summary>
    public sealed class KeylessKeyedAttributeHandler : QueryHandler<CaptiveQuery, CaptiveResult>
    {
        public KeylessKeyedAttributeHandler([FromKeyedServices] IKeyedDb db)
        {
        }

        public override CaptiveResult Execute(CaptiveQuery query) => new CaptiveResult();
    }
}
