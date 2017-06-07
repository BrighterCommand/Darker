using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker;
using Paramore.Darker.Policies;
using Paramore.Darker.QueryLogging;
using SampleApi.Domain;
using SampleApi.Ports.Queries;

namespace SampleApi.Ports.Handlers
{
    public sealed class GetPeopleHandler : QueryHandlerAsync<GetPeople, IReadOnlyDictionary<int, string>>
    {
        [QueryLogging(1)]
        [RetryableQuery(2, Startup.SomethingWentTerriblyWrongCircuitBreakerName)]
        public override async Task<IReadOnlyDictionary<int, string>> ExecuteAsync(GetPeople query, CancellationToken cancellationToken = default(CancellationToken))
        {
            var repository = new PersonRepository();
            return await repository.GetAll(cancellationToken);
        }
    }
}
