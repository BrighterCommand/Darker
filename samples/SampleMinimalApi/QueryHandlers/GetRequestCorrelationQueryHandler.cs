using Paramore.Darker;
using SampleMinimalApi.Domain;

namespace SampleMinimalApi.QueryHandlers;

public sealed class GetRequestCorrelationQuery : IQuery<Guid>
{
}

public sealed class GetRequestCorrelationQueryHandler(RequestCorrelation correlation)
    : QueryHandlerAsync<GetRequestCorrelationQuery, Guid>
{
    public override Task<Guid> ExecuteAsync(GetRequestCorrelationQuery query,
        CancellationToken cancellationToken = default) => Task.FromResult(correlation.Id);
}
