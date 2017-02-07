using System;

namespace Darker
{
    public interface IQueryHandlerRegistry
    {
        Type Get(Type queryType);

        void Register<TQuery, TResult, THandler>()
            where TQuery : IQuery<TResult>
            where THandler : IQueryHandler<TQuery, TResult>;

        void Register(Type queryType, Type resultType, Type handlerType);
    }
}