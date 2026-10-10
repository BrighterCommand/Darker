using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class FirstThrowOnDisposeAttribute : QueryHandlerAttribute
    {
        public FirstThrowOnDisposeAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(FirstThrowOnDisposeDecorator<,>);
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class SecondThrowOnDisposeAttribute : QueryHandlerAttribute
    {
        public SecondThrowOnDisposeAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(SecondThrowOnDisposeDecorator<,>);
    }

    /// <summary>A pass-through decorator whose <see cref="Dispose"/> throws the fault it was given.</summary>
    internal sealed class FirstThrowOnDisposeDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>, IDisposable
        where TQuery : IQuery<TResult>
    {
        private readonly Exception _fault;

        public FirstThrowOnDisposeDecorator(Exception fault) => _fault = fault;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback) => next(query);

        public void Dispose() => throw _fault;
    }

    /// <summary>A second, distinct pass-through decorator whose <see cref="Dispose"/> throws.</summary>
    internal sealed class SecondThrowOnDisposeDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>, IDisposable
        where TQuery : IQuery<TResult>
    {
        private readonly Exception _fault;

        public SecondThrowOnDisposeDecorator(Exception fault) => _fault = fault;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback) => next(query);

        public void Dispose() => throw _fault;
    }
}
