using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker
{
    public interface IQueryHandlerAsync<in TQuery, TResult> : IQueryHandler
        where TQuery : IQuery<TResult>
    {
        Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken));

        Task<TResult> FallbackAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken));
    }
}
