using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A handler whose constructor parameter is bound to the keyed registration "k".</summary>
    public sealed class KeyedCaptiveHandler : QueryHandler<CaptiveQuery, CaptiveResult>
    {
        public KeyedCaptiveHandler([FromKeyedServices("k")] IKeyedDb db)
        {
        }

        public override CaptiveResult Execute(CaptiveQuery query) => new CaptiveResult();
    }
}
