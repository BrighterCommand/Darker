namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    public sealed class AsyncOnlyDisposableQueryHandler : QueryHandler<AsyncOnlyDisposableQuery, AsyncOnlyDisposableResult>
    {
        public const string RESULT = "async-only";

        private readonly AsyncOnlyDisposableDependency _dependency;

        public AsyncOnlyDisposableQueryHandler(AsyncOnlyDisposableDependency dependency) => _dependency = dependency;

        public override AsyncOnlyDisposableResult Execute(AsyncOnlyDisposableQuery query)
            => new AsyncOnlyDisposableResult(_dependency != null ? RESULT : null);
    }
}
