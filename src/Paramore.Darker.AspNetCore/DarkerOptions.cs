namespace Paramore.Darker.AspNetCore
{
    public sealed class DarkerOptions
    {
        public IQueryContextFactory QueryContextFactory { get; set; } = new InMemoryQueryContextFactory();
    }
}