using System;
using System.Threading.Tasks;

namespace Darker
{
    public interface IQueryHandlerDecorator
    {
        IRequestContext Context { get; set; }
        void InitializeFromAttributeParams(object[] attributeParams);
    }

    public interface IQueryHandlerDecorator<TRequest, TResponse> : IQueryHandlerDecorator
        where TRequest : IQueryRequest<TResponse>
        where TResponse : IQueryResponse
    {
        TResponse Execute(TRequest request, Func<TRequest, TResponse> next, Func<TRequest, TResponse> fallback);
        Task<TResponse> ExecuteAsync(TRequest request, Func<TRequest, Task<TResponse>> next, Func<TRequest, Task<TResponse>> fallback);
    }
}