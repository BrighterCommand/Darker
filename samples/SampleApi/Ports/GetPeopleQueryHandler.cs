using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Darker;
using Darker.Policies;
using Darker.RequestLogging;
using SampleApi.Domain;

namespace SampleApi.Ports
{
    public sealed class GetPeopleQuery : IQuery<GetPeopleQuery.Result>
    {
        public sealed class Result
        {
            public IReadOnlyDictionary<int, string> People { get; }

            public Result(IReadOnlyDictionary<int, string> people)
            {
                People = people;
            }
        }
    }

    public sealed class GetPeopleQueryHandler : AsyncQueryHandler<GetPeopleQuery, GetPeopleQuery.Result>
    {
        [RequestLogging(1)]
        [RetryableQuery(2, Startup.SomethingWentTerriblyWrongCircuitBreakerName)]
        public override async Task<GetPeopleQuery.Result> ExecuteAsync(GetPeopleQuery query, CancellationToken cancellationToken = new CancellationToken())
        {
            var repository = new PersonRepository();
            var people = await repository.GetAll(cancellationToken);

            return new GetPeopleQuery.Result(people);
        }
    }
}
