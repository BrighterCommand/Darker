using System;
using System.Threading;
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

    public sealed class AsyncOnlyDisposableQuery : IQuery<AsyncOnlyDisposableResult>
    {
    }

    public sealed class AsyncOnlyDisposableResult
    {
        public AsyncOnlyDisposableResult(string value) => Value = value;

        public string Value { get; }
    }

    public sealed class AsyncOnlyDisposableQueryHandler : QueryHandler<AsyncOnlyDisposableQuery, AsyncOnlyDisposableResult>
    {
        public const string RESULT = "async-only";

        private readonly AsyncOnlyDisposableDependency _dependency;

        public AsyncOnlyDisposableQueryHandler(AsyncOnlyDisposableDependency dependency) => _dependency = dependency;

        public override AsyncOnlyDisposableResult Execute(AsyncOnlyDisposableQuery query)
            => new AsyncOnlyDisposableResult(_dependency != null ? RESULT : null);
    }
}
