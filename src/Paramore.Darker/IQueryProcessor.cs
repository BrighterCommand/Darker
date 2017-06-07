using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker
{
    public interface IQueryProcessor
    {
        TResult Execute<TResult>(IQuery<TResult> query);

        Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default(CancellationToken));

#if NETSTANDARD
        Task<TResult> ExecuteRemoteAsync<TResult>(IRemoteQuery<TResult> query, CancellationToken cancellationToken = default(CancellationToken));
#endif
    }
}