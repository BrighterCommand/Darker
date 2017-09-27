using System;
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Testing.Ports
{
    public class TestQueryHandler : IQueryHandler<TestQueryA, Guid>
    {
        public IQueryContext Context { get; set; }

        public Guid Execute(TestQueryA query)
        {
            Context.Bag.Add("id", query.Id);
            return query.Id;
        }

        public Guid Fallback(TestQueryA query)
        {
            throw new NotImplementedException();
        }

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