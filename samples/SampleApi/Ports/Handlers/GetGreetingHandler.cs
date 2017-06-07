using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker;
using Paramore.Darker.QueryLogging;
using SampleApi.Ports.Queries;

namespace SampleApi.Ports.Handlers
{
    public sealed class GetGreetingHandler : QueryHandlerAsync<GetGreeting, string>
    {
        [QueryLogging(1)]
        public override Task<string> ExecuteAsync(GetGreeting query, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult($"Hello, {query.Name}!");
        }
    }
}