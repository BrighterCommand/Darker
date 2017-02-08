using System.Threading;
using System.Threading.Tasks;

namespace Darker
{
    public interface IQueryHandler
    {
        IQueryContext Context { get; set; }
    }

    public interface IQueryHandler<in TQuery, TResult> : IQueryHandler
        where TQuery : IQuery<TResult>
    {
        TResult Execute(TQuery query);

        TResult Fallback(TQuery query);

        Task<TResult> ExecuteAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken));

        Task<TResult> FallbackAsync(TQuery query, CancellationToken cancellationToken = default(CancellationToken));
    }
}