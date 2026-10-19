using System;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A service that implements only <see cref="IAsyncDisposable"/>, so a container cannot dispose
    /// it synchronously. Used with its own query so the pipeline's closed generic is disjoint.
    /// </summary>
    public sealed class AsyncOnlyDisposableDependency : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => default;
    }
}
