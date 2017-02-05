using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darker;
using Darker.Policies;
using Darker.RequestLogging;
using SampleApi.Domain;

namespace SampleApi.Ports
{
    public sealed class GetPeopleQuery : IQueryRequest<GetPeopleQuery.Response>
    {
        public sealed class Response : IQueryResponse
        {
            public IReadOnlyDictionary<int, string> People { get; }

            public Response(IReadOnlyDictionary<int, string> people)
            {
                People = people;
            }
        }
    }

    public sealed class GetPeopleQueryHandler : AsyncQueryHandler<GetPeopleQuery, GetPeopleQuery.Response>
    {
        [RequestLogging(1)]
        [RetryableQuery(2, Startup.SomethingWentTerriblyWrongCircuitBreakerName)]
        public override async Task<GetPeopleQuery.Response> ExecuteAsync(GetPeopleQuery request, CancellationToken cancellationToken = new CancellationToken())
        {
            var repository = new PersonRepository();
            var people = await repository.GetAll(cancellationToken);

            return new GetPeopleQuery.Response(people);
        }
    }
}
