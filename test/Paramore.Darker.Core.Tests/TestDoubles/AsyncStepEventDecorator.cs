using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// Attribute that wires <see cref="AsyncStepEventDecorator{TQuery,TResult}"/> into the
    /// async pipeline for use in step-event tracing tests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class AsyncStepEventAttribute : QueryHandlerAttributeAsync
    {
        public AsyncStepEventAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(AsyncStepEventDecorator<,>);
    }

    /// <summary>
    /// A minimal pass-through async decorator used to verify that <c>PipelineBuilder.BuildAsync</c>
    /// writes a step event for each decorator in the async pipeline. Delegates unconditionally to
    /// <paramref name="next"/> so the query executes normally.
    /// </summary>
    internal sealed class AsyncStepEventDecorator<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default)
            => next(query, cancellationToken);
    }
}
