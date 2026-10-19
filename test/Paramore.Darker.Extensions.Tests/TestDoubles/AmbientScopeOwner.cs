using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// Stands for whoever owns the ambient scope (the web server, in a real host). It is a singleton, so a
    /// handler can reach it and dispose the ambient scope in the middle of a query, as a careless owner might.
    /// </summary>
    public sealed class AmbientScopeOwner
    {
        public IServiceScope Scope { get; set; }

        public void DisposeAmbientScope() => Scope.Dispose();
    }
}
