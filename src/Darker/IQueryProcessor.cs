using System.Threading;
using System.Threading.Tasks;

namespace Darker
{
    public interface IQueryProcessor
    {
        TResult Execute<TResult>(IQuery<TResult> query);

        Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default(CancellationToken));
    }
}