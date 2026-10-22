using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Observability;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    public sealed class DarkerOptions
    {
        public IQueryContextFactory QueryContextFactory { get; set; } = new InMemoryQueryContextFactory();
        public ServiceLifetime HandlerLifetime { get; set; } = ServiceLifetime.Transient;
        public ServiceLifetime QueryProcessorLifetime { get; set; } = ServiceLifetime.Singleton;
        public InstrumentationOptions InstrumentationOptions { get; set; } = InstrumentationOptions.All;
        /// <summary>
        /// Gets or sets the affinity used when none is registered with <c>AddDarkerScopeProvider</c> or
        /// <c>AddDarkerRequestScope</c>; a registered affinity overrides it. The default is
        /// <see cref="ScopeAffinity.AlwaysNew"/>.
        /// </summary>
        public ScopeAffinity DefaultScopeAffinity { get; set; } = ScopeAffinity.AlwaysNew;
    }
}