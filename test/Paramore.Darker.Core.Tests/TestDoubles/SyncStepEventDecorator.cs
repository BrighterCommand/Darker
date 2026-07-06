using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// Attribute that wires <see cref="SyncStepEventDecorator{TQuery,TResult}"/> into the
    /// pipeline for use in step-event tracing tests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class SyncStepEventAttribute : QueryHandlerAttribute
    {
        public SyncStepEventAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(SyncStepEventDecorator<,>);
    }

    /// <summary>
    /// A minimal pass-through decorator used to verify that <c>PipelineBuilder.Build</c> writes
    /// a step event for each decorator in the sync pipeline. Delegates unconditionally to
    /// <paramref name="next"/> so the query executes normally.
    /// </summary>
    internal sealed class SyncStepEventDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
            => next(query);
    }
}
