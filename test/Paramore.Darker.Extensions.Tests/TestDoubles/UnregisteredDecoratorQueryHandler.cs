namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler whose Execute method carries an attribute for an unregistered decorator.</summary>
    public sealed class UnregisteredDecoratorQueryHandler : QueryHandler<UnregisteredDecoratorQuery, UnregisteredDecoratorResult>
    {
        [UnregisteredDecorator(1)]
        public override UnregisteredDecoratorResult Execute(UnregisteredDecoratorQuery query) => new UnregisteredDecoratorResult();
    }
}
