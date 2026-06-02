using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Logging.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A query with observable public properties so the query logging decorator's serialised
    /// <c>{Query}</c> output is meaningful. Reserved for the Core.Tests logging tests so its
    /// <c>QueryLoggingDecorator&lt;CoreLoggingTestQuery, …&gt;</c> closed generic is a disjoint cache
    /// cell from the Extensions.Tests equivalent (FR10 cross-assembly discipline).
    /// </summary>
    internal sealed class CoreLoggingTestQuery : IQuery<CoreLoggingTestQuery.Result>
    {
        public Guid Id { get; init; }

        public string Name { get; init; }

        internal sealed class Result
        {
            public Guid Value { get; set; }
        }
    }

    internal sealed class CoreLoggingTestQueryHandler : QueryHandler<CoreLoggingTestQuery, CoreLoggingTestQuery.Result>
    {
        [QueryLogging(1)]
        public override CoreLoggingTestQuery.Result Execute(CoreLoggingTestQuery query)
            => new CoreLoggingTestQuery.Result { Value = query.Id };
    }

    internal sealed class CoreLoggingTestQueryHandlerAsync : QueryHandlerAsync<CoreLoggingTestQuery, CoreLoggingTestQuery.Result>
    {
        [QueryLoggingAttributeAsync(1)]
        public override Task<CoreLoggingTestQuery.Result> ExecuteAsync(CoreLoggingTestQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new CoreLoggingTestQuery.Result { Value = query.Id });
    }
}
