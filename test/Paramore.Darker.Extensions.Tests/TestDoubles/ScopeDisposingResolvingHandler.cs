using System;
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// Has the ambient scope disposed while it runs, then resolves a service from the provider it was
    /// given, which is that scope's own provider.
    /// </summary>
    public sealed class ScopeDisposingResolvingHandler : QueryHandler<ScopeDisposingResolvingQuery, SharedRecordingResult>
    {
        private readonly AmbientScopeOwner _owner;
        private readonly IServiceProvider _services;

        public ScopeDisposingResolvingHandler(AmbientScopeOwner owner, IServiceProvider services)
        {
            _owner = owner;
            _services = services;
        }

        public override SharedRecordingResult Execute(ScopeDisposingResolvingQuery query)
        {
            _owner.DisposeAmbientScope();
            _services.GetRequiredService<ITrackedDependency>();
            return new SharedRecordingResult();
        }
    }
}
