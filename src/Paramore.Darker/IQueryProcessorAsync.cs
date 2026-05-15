using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker
{
    public interface IQueryProcessorAsync
    {
        Task<TResult> ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default(CancellationToken));
    }
}
