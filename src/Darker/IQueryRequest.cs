namespace Darker
{
    public interface IQueryRequest
    {
    }

    public interface IQueryRequest<TResponse> : IQueryRequest
        where TResponse : IQueryResponse
    {
    }
}