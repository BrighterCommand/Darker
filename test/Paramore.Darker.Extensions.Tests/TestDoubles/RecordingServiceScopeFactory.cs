using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A scope factory over the real one that counts how many scopes were created.</summary>
    public sealed class RecordingServiceScopeFactory : IServiceScopeFactory
    {
        private readonly IServiceScopeFactory _inner;
        private int _scopesCreated;

        public RecordingServiceScopeFactory(IServiceScopeFactory inner) => _inner = inner;

        public int ScopesCreated => Volatile.Read(ref _scopesCreated);

        public IServiceScope CreateScope()
        {
            Interlocked.Increment(ref _scopesCreated);
            return _inner.CreateScope();
        }
    }
}
