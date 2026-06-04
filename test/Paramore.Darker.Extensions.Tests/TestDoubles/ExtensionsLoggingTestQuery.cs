using System;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker;
using Paramore.Darker.Logging.Attributes;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// The Extensions.Tests counterpart of <c>CoreLoggingTestQuery</c>: a query with observable public
    /// properties whose <c>QueryLoggingDecorator&lt;ExtensionsLoggingTestQuery, …&gt;</c> closed generic is a
    /// disjoint cache cell from the Core.Tests one (FR10 cross-assembly discipline).
    /// </summary>
    public sealed class ExtensionsLoggingTestQuery : IQuery<ExtensionsLoggingTestQuery.Result>
    {
        public Guid Id { get; init; }

        public string Name { get; init; }

        public sealed class Result
        {
            public Guid Value { get; set; }
        }
    }

    public sealed class ExtensionsLoggingTestQueryHandler : QueryHandler<ExtensionsLoggingTestQuery, ExtensionsLoggingTestQuery.Result>
    {
        [QueryLogging(1)]
        public override ExtensionsLoggingTestQuery.Result Execute(ExtensionsLoggingTestQuery query)
            => new ExtensionsLoggingTestQuery.Result { Value = query.Id };
    }

    public sealed class ExtensionsLoggingTestQueryHandlerAsync : QueryHandlerAsync<ExtensionsLoggingTestQuery, ExtensionsLoggingTestQuery.Result>
    {
        [QueryLoggingAttributeAsync(1)]
        public override Task<ExtensionsLoggingTestQuery.Result> ExecuteAsync(ExtensionsLoggingTestQuery query,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ExtensionsLoggingTestQuery.Result { Value = query.Id });
    }
}
