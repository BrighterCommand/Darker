using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// A query whose handler <em>and</em> decorator both receive an <see cref="ITrackedDependency"/>
    /// by constructor injection. The handler records the dependency it received and the decorator
    /// records the one it received into the same result, so a test can assert whether the two share
    /// one instance (scoped) and whether they are disposed after the pipeline (AC4, AC8).
    /// </summary>
    public sealed class DecoratedTrackedQuery : IQuery<DecoratedTrackedQuery.Result>
    {
        public sealed class Result
        {
            public ITrackedDependency HandlerDependency { get; set; }

            public ITrackedDependency DecoratorDependency { get; set; }
        }
    }

    public sealed class DecoratedTrackedQueryHandler : QueryHandler<DecoratedTrackedQuery, DecoratedTrackedQuery.Result>
    {
        private readonly ITrackedDependency _dependency;

        public DecoratedTrackedQueryHandler(ITrackedDependency dependency) => _dependency = dependency;

        [TrackedDecorator(1)]
        public override DecoratedTrackedQuery.Result Execute(DecoratedTrackedQuery query)
            => new DecoratedTrackedQuery.Result { HandlerDependency = _dependency };
    }

    public sealed class DecoratedTrackedQueryHandlerAsync : QueryHandlerAsync<DecoratedTrackedQuery, DecoratedTrackedQuery.Result>
    {
        private readonly ITrackedDependency _dependency;

        public DecoratedTrackedQueryHandlerAsync(ITrackedDependency dependency) => _dependency = dependency;

        [TrackedDecoratorAsync(1)]
        public override Task<DecoratedTrackedQuery.Result> ExecuteAsync(DecoratedTrackedQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new DecoratedTrackedQuery.Result { HandlerDependency = _dependency });
    }

    public sealed class TrackedDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly ITrackedDependency _dependency;

        public TrackedDecorator(ITrackedDependency dependency) => _dependency = dependency;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
        {
            var result = next(query);
            if (result is DecoratedTrackedQuery.Result tracked)
                tracked.DecoratorDependency = _dependency;
            return result;
        }
    }

    public sealed class TrackedDecoratorAsync<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly ITrackedDependency _dependency;

        public TrackedDecoratorAsync(ITrackedDependency dependency) => _dependency = dependency;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public async Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default)
        {
            var result = await next(query, cancellationToken).ConfigureAwait(false);
            if (result is DecoratedTrackedQuery.Result tracked)
                tracked.DecoratorDependency = _dependency;
            return result;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TrackedDecoratorAttribute : QueryHandlerAttribute
    {
        public TrackedDecoratorAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(TrackedDecorator<,>);
    }

    [AttributeUsage(AttributeTargets.Method)]
    public sealed class TrackedDecoratorAsyncAttribute : QueryHandlerAttributeAsync
    {
        public TrackedDecoratorAsyncAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(TrackedDecoratorAsync<,>);
    }
}
