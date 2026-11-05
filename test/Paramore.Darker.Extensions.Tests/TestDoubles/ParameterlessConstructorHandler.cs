namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler whose only public constructor takes no parameters.</summary>
    public sealed class ParameterlessConstructorHandler : QueryHandler<CaptiveQuery, CaptiveResult>
    {
        public override CaptiveResult Execute(CaptiveQuery query) => new CaptiveResult();
    }
}
