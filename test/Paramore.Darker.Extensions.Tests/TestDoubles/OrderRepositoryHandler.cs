namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler whose only constructor takes an <see cref="IRepository{T}"/> of <see cref="Order"/>.</summary>
    public sealed class OrderRepositoryHandler : QueryHandler<CaptiveQuery, CaptiveResult>
    {
        public OrderRepositoryHandler(IRepository<Order> repository)
        {
        }

        public override CaptiveResult Execute(CaptiveQuery query) => new CaptiveResult();
    }
}
