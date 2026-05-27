using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.Exported
{
    public class TestQueryHandlerAsync : IQueryHandlerAsync<TestQueryA, Guid>
    {
        public IQueryContext Context { get; set; }

        public Task<Guid> ExecuteAsync(TestQueryA query, CancellationToken cancellationToken = default(CancellationToken))
        {
            Context.Bag.Add("id", query.Id);
            return Task.FromResult(query.Id);
        }

        public Task<Guid> FallbackAsync(TestQueryA query, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException();
        }
    }
}
