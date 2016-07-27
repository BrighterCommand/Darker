namespace Darker
{
    public interface IQueryProcessor
    {
        TResponse Execute<TResponse>(IQueryRequest<TResponse> request)
            where TResponse : IQueryResponse;
    }
}