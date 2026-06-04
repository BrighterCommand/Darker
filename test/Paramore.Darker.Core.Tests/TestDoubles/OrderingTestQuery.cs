using Paramore.Darker.Logging.Attributes;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A minimal query reserved exclusively for the FR14 lock-after-use ordering test (AC3). Its
    /// <c>QueryLoggingDecorator&lt;OrderingTestQuery, …&gt;</c> closed generic is a disjoint cache cell
    /// that no other test touches, so the irreversible lock that test triggers on
    /// <c>QueryLoggingJsonOptions.Options</c> cannot leak into another test through a shared generic.
    /// The single <c>Marker</c> property serialises to the exact indented form the test asserts.
    /// </summary>
    public sealed class OrderingTestQuery : IQuery<OrderingTestQuery.Result>
    {
        public string Marker { get; init; } = "x";

        public sealed class Result
        {
        }
    }

    internal sealed class OrderingTestQueryHandler : QueryHandler<OrderingTestQuery, OrderingTestQuery.Result>
    {
        [QueryLogging(1)]
        public override OrderingTestQuery.Result Execute(OrderingTestQuery query)
            => new OrderingTestQuery.Result();
    }
}
