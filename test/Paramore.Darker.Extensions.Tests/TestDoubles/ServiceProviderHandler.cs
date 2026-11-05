namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>A public handler whose only constructor takes an <see cref="System.IServiceProvider"/>.</summary>
    public sealed class ServiceProviderHandler : QueryHandler<CaptiveQuery, CaptiveResult>
    {
        public ServiceProviderHandler(System.IServiceProvider dependency)
        {
        }

        public override CaptiveResult Execute(CaptiveQuery query) => new CaptiveResult();
    }
}
