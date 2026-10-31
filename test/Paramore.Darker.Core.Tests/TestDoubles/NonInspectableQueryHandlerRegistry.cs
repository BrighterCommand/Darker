using System;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>A custom sync registry that does not implement <see cref="IAmAHandlerRegistryInspector"/>.</summary>
    public sealed class NonInspectableQueryHandlerRegistry : IQueryHandlerRegistry
    {
        public Type Get(Type queryType, IQuery query, IQueryContext context) => throw new NotSupportedException();

        public void Register<TQuery, TResult, THandler>()
            where TQuery : IQuery<TResult>
            where THandler : IQueryHandler<TQuery, TResult> => throw new NotSupportedException();

        public void Register(Type queryType, Type resultType, Type handlerType) => throw new NotSupportedException();

        public void Register<TQuery, TResult>(
            Func<TQuery, IQueryContext, Type?> router,
            params Type[] candidateHandlerTypes)
            where TQuery : IQuery<TResult> => throw new NotSupportedException();
    }
}
