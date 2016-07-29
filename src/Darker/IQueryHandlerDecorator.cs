using System;

namespace Darker
{
    public interface IQueryHandlerDecorator<TRequest, TResponse>
        where TRequest : IQueryRequest<TResponse>
        where TResponse : IQueryResponse
    {
        IRequestContext Context { get; set; }

        void InitializeFromAttributeParams(object[] attributeParams);

        TResponse Execute(TRequest request, Func<TRequest, TResponse> next, Func<TRequest, TResponse> fallback);
    }
}