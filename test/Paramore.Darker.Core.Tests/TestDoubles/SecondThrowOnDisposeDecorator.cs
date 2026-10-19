using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
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
