using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class TrackedStreamQueryHandler : StreamQueryHandler<TrackedStreamQuery, string>
    {
        private readonly ITrackedDependency _dependency;

        public TrackedStreamQueryHandler(ITrackedDependency dependency) => _dependency = dependency;

        [TrackedStreamDecorator(1)]
        public override async IAsyncEnumerable<string> ExecuteAsync(
            TrackedStreamQuery query, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            query.HandlerDependency = _dependency;
            query.SignalStarted();
            await query.ReleaseGate.ConfigureAwait(false);
            yield return "item";
        }
    }
}
