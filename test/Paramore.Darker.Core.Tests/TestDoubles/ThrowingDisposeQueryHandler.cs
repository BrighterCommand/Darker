using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A decorated handler whose <see cref="Dispose"/> throws the supplied fault.</summary>
    internal sealed class ThrowingDisposeQueryHandler : QueryHandler<ThrowingDisposeQuery, ThrowingDisposeResult>, IDisposable
    {
        public const string RESULT = "survived-dispose-fault";

        private readonly Exception _disposeFault;

        public ThrowingDisposeQueryHandler(Exception disposeFault) => _disposeFault = disposeFault;

        [FirstPassThrough(step: 1)]
        [SecondPassThrough(step: 2)]
        public override ThrowingDisposeResult Execute(ThrowingDisposeQuery query) => new ThrowingDisposeResult(RESULT);

        public void Dispose() => throw _disposeFault;
    }
}
