using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A decorator whose constructor records that the container built it.</summary>
    public sealed class ConstructionRecordingDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        public ConstructionRecordingDecorator(ConstructionCounter counter) => counter.DecoratorConstructed();

        public IQueryContext Context { get; set; }

        public void InitializeFromAttributeParams(object[] attributeParams) { }

        public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback) => next(query);
    }
}
