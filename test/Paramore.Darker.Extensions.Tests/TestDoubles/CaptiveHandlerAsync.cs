using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public async handler whose only constructor takes a <see cref="ScopedDependency"/>.</summary>
    public sealed class CaptiveHandlerAsync : QueryHandlerAsync<CaptiveQuery, CaptiveResult>
    {
        public CaptiveHandlerAsync(ScopedDependency dependency)
        {
        }

        public override Task<CaptiveResult> ExecuteAsync(CaptiveQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(new CaptiveResult());
    }
}
