using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A handler carrying two decorators; its own <see cref="Dispose"/> throws as well.</summary>
    internal sealed class EveryReleaseThrowsQueryHandler : QueryHandler<EveryReleaseThrowsQuery, EveryReleaseThrowsResult>, IDisposable
    {
        public const string RESULT = "survived-every-release-fault";

        private readonly Exception _disposeFault;

        public EveryReleaseThrowsQueryHandler(Exception disposeFault) => _disposeFault = disposeFault;

        [FirstThrowOnDispose(step: 1)]
        [SecondThrowOnDispose(step: 2)]
        public override EveryReleaseThrowsResult Execute(EveryReleaseThrowsQuery query) => new EveryReleaseThrowsResult(RESULT);

        public void Dispose() => throw _disposeFault;
    }
}
