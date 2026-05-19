using System;

namespace Paramore.Darker
{
    public interface IQueryHandlerRegistryAsync
    {
        Type Get(Type queryType);

        void Register<TQuery, TResult, THandler>()
            where TQuery : IQuery<TResult>
            where THandler : IQueryHandlerAsync<TQuery, TResult>;

        void Register(Type queryType, Type resultType, Type handlerType);
    }
}
