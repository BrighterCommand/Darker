using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
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
}
