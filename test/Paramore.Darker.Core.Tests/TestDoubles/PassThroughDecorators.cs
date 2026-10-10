using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class FirstPassThroughAttribute : QueryHandlerAttribute
    {
        public FirstPassThroughAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(FirstPassThroughDecorator<,>);
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class SecondPassThroughAttribute : QueryHandlerAttribute
    {
        public SecondPassThroughAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(SecondPassThroughDecorator<,>);
    }

    /// <summary>A decorator that calls the next step unchanged.</summary>
    internal sealed class FirstPassThroughDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback) => next(query);
    }

    /// <summary>A second, distinct pass-through decorator, so a handler can carry two decorators.</summary>
    internal sealed class SecondPassThroughDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback) => next(query);
    }
}
