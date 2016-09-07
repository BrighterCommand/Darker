namespace Darker
{
    public interface IQueryHandler
    {
    }

    public interface IQueryHandler<in TRequest, out TResponse> : IQueryHandler
        where TRequest : IQueryRequest<TResponse>
        where TResponse : IQueryResponse
    {
        IRequestContext Context { get; set; }

        TResponse Execute(TRequest request);

        TResponse Fallback(TRequest request);
    }
}