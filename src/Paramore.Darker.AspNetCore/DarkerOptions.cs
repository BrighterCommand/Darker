using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.AspNetCore
{
    public sealed class DarkerOptions
    {
        public IQueryContextFactory QueryContextFactory { get; set; } = new InMemoryQueryContextFactory();
        public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;
        public ServiceLifetime QueryProcessorLifetime { get; set; } = ServiceLifetime.Singleton;
    }
}