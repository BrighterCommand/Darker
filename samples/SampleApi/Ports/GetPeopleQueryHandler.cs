using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker;
using Paramore.Darker.Policies;
using Paramore.Darker.QueryLogging;
using SampleApi.Domain;

namespace SampleApi.Ports
{
    public sealed class GetPeopleQuery : IQuery<IReadOnlyDictionary<int, string>>
    {
    }

    public sealed class GetPeopleQueryHandler : QueryHandlerAsync<GetPeopleQuery, IReadOnlyDictionary<int, string>>
    {
        [QueryLogging(1)]
        [RetryableQuery(2, Startup.SomethingWentTerriblyWrongCircuitBreakerName)]
        public override async Task<IReadOnlyDictionary<int, string>> ExecuteAsync(GetPeopleQuery query, CancellationToken cancellationToken = default(CancellationToken))
        {
            var repository = new PersonRepository();
            return await repository.GetAll(cancellationToken);
        }
    }
}
