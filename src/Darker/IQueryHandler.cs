using System.Threading.Tasks;

namespace Darker
{
    public interface IQueryHandler
    {
        IRequestContext Context { get; set; }
    }

    public interface IQueryHandler<in TRequest, TResponse> : IQueryHandler
        where TRequest : IQueryRequest<TResponse>
        where TResponse : IQueryResponse
    {
        TResponse Execute(TRequest request);

        TResponse Fallback(TRequest request);

        Task<TResponse> ExecuteAsync(TRequest request);

        Task<TResponse> FallbackAsync(TRequest request);
    }
}