using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.Exported;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>An async handler that implements the interface's members only explicitly, so it has no public ExecuteAsync.</summary>
    internal sealed class ExplicitInterfaceAsyncHandler : IQueryHandlerAsync<TestQueryA, Guid>
    {
        public IQueryContext Context { get; set; }

        Task<Guid> IQueryHandlerAsync<TestQueryA, Guid>.ExecuteAsync(TestQueryA query, CancellationToken cancellationToken)
            => Task.FromResult(query.Id);

        Task<Guid> IQueryHandlerAsync<TestQueryA, Guid>.FallbackAsync(TestQueryA query, CancellationToken cancellationToken)
            => throw new NotImplementedException();
    }
}
