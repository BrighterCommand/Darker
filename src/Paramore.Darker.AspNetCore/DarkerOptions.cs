using System.Reflection;

namespace Paramore.Darker.AspNetCore
{
    public sealed class DarkerOptions
    {
        public IQueryContextFactory QueryContextFactory { get; set; } = new InMemoryQueryContextFactory();
        public Assembly[] DiscoverQueriesAndHandlersFromAssemblies { get; set; } = new Assembly[0];
    }
}