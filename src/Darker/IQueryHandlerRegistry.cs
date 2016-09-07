using System;

namespace Darker
{
    public interface IQueryHandlerRegistry
    {
        Type Get(Type requestType);

        void Register<TRequest, TResponse, THandler>()
            where TRequest : IQueryRequest<TResponse>
            where TResponse : IQueryResponse
            where THandler : IQueryHandler<TRequest, TResponse>;

        void RegisterAsync<TRequest, TResponse, THandler>()
            where TRequest : IQueryRequest<TResponse>
            where TResponse : IQueryResponse
            where THandler : IAsyncQueryHandler<TRequest, TResponse>;

        void Register(Type requestType, Type responseType, Type handlerType);
    }
}