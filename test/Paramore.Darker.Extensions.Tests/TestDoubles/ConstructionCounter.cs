using System.Threading;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>An injected tally of how many handlers and decorators the container has constructed.</summary>
    public sealed class ConstructionCounter
    {
        private int _handlers;
        private int _decorators;
        private int _scopeProviders;

        public int Handlers => Volatile.Read(ref _handlers);

        public int Decorators => Volatile.Read(ref _decorators);

        public int ScopeProviders => Volatile.Read(ref _scopeProviders);

        public void HandlerConstructed() => Interlocked.Increment(ref _handlers);

        public void DecoratorConstructed() => Interlocked.Increment(ref _decorators);

        public void ScopeProviderConstructed() => Interlocked.Increment(ref _scopeProviders);
    }
}
