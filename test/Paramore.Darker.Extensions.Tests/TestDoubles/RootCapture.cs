using System;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// Captures the <see cref="IServiceProvider"/> a singleton factory delegate is given. For a singleton
    /// that is the container's root, which a careless adapter might offer as an ambient scope.
    /// </summary>
    public sealed class RootCapture(IServiceProvider services)
    {
        public IServiceProvider Services { get; } = services;
    }
}
