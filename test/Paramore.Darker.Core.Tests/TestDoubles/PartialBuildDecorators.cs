using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    // Decorators for the partial-build tests. Each pipeline kind has a pass-through decorator and a
    // decorator whose InitializeFromAttributeParams throws the fault it was constructed with.
    // The sync pass-through decorator is FirstPassThroughDecorator.

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ThrowingInitAttribute : QueryHandlerAttribute
    {
        public ThrowingInitAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(ThrowingInitDecorator<,>);
    }

    internal sealed class ThrowingInitDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly Exception _fault;

        public ThrowingInitDecorator(Exception fault) => _fault = fault;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) => throw _fault;

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback) => next(query);
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class PassThroughAsyncAttribute : QueryHandlerAttributeAsync
    {
        public PassThroughAsyncAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(PassThroughAsyncDecorator<,>);
    }

    internal sealed class PassThroughAsyncDecorator<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
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

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ThrowingInitAsyncAttribute : QueryHandlerAttributeAsync
    {
        public ThrowingInitAsyncAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(ThrowingInitAsyncDecorator<,>);
    }

    internal sealed class ThrowingInitAsyncDecorator<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        private readonly Exception _fault;

        public ThrowingInitAsyncDecorator(Exception fault) => _fault = fault;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) => throw _fault;

        public Task<TResult> ExecuteAsync(TQuery query,
            Func<TQuery, CancellationToken, Task<TResult>> next,
            Func<TQuery, CancellationToken, Task<TResult>> fallback,
            CancellationToken cancellationToken = default)
            => next(query, cancellationToken);
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class PassThroughStreamAttribute : StreamQueryHandlerAttribute
    {
        public PassThroughStreamAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(PassThroughStreamDecorator<,>);
    }

    internal sealed class PassThroughStreamDecorator<TQuery, TResult> : IStreamQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IStreamQuery<TResult>
    {
        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public IAsyncEnumerable<TResult> Execute(TQuery query,
            Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
            CancellationToken cancellationToken)
            => next(query, cancellationToken);
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ThrowingInitStreamAttribute : StreamQueryHandlerAttribute
    {
        public ThrowingInitStreamAttribute(int step) : base(step) { }

        public override object[] GetAttributeParams() => [];

        public override Type GetDecoratorType() => typeof(ThrowingInitStreamDecorator<,>);
    }

    internal sealed class ThrowingInitStreamDecorator<TQuery, TResult> : IStreamQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IStreamQuery<TResult>
    {
        private readonly Exception _fault;

        public ThrowingInitStreamDecorator(Exception fault) => _fault = fault;

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) => throw _fault;

        public IAsyncEnumerable<TResult> Execute(TQuery query,
            Func<TQuery, CancellationToken, IAsyncEnumerable<TResult>> next,
            CancellationToken cancellationToken)
            => next(query, cancellationToken);
    }
}
