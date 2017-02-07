using System.Threading;
using System.Threading.Tasks;
using Darker;
using Darker.Attributes;
using Darker.Policies;
using Darker.RequestLogging;
using SampleApi.Domain;

namespace SampleApi.Ports
{
    public sealed class GetPersonQuery : IQuery<GetPersonQuery.Result>
    {
        public int PersonId { get; }

        public GetPersonQuery(int personId)
        {
            PersonId = personId;
        }

        public sealed class Result
        {
            public string Name { get; }

            public Result(string name)
            {
                Name = name;
            }
        }
    }

    public sealed class GetPersonQueryHandler : AsyncQueryHandler<GetPersonQuery, GetPersonQuery.Result>
    {
        [RequestLogging(1)]
        [FallbackPolicy(2)]
        [RetryableQuery(3, Startup.SomethingWentTerriblyWrongCircuitBreakerName)]
        public override async Task<GetPersonQuery.Result> ExecuteAsync(GetPersonQuery query, CancellationToken cancellationToken = new CancellationToken())
        {
            var repository = new PersonRepository();
            var person = await repository.GetById(query.PersonId, cancellationToken);

            return new GetPersonQuery.Result(person);
        }

        public override Task<GetPersonQuery.Result> FallbackAsync(GetPersonQuery query, CancellationToken cancellationToken = new CancellationToken())
        {
            // this will happen when the circuit is broken or when the id doesn't exist
            return Task.FromResult(new GetPersonQuery.Result("Linus Torvalds"));
        }
    }
}
