using System;
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.Tests.TestDoubles
{
    /// <summary>
    /// Wraps a provider so that its <see cref="IServiceScopeFactory"/> counts scope creation; every other request passes through.
    /// </summary>
    public sealed class ScopeRecordingServiceProvider : IServiceProvider, ISupportRequiredService
    {
        private readonly IServiceProvider _inner;

        public ScopeRecordingServiceProvider(IServiceProvider inner)
        {
            _inner = inner;
            ScopeFactory = new RecordingServiceScopeFactory(inner.GetRequiredService<IServiceScopeFactory>());
        }

        public RecordingServiceScopeFactory ScopeFactory { get; }

        public object GetService(Type serviceType) =>
            serviceType == typeof(IServiceScopeFactory) ? ScopeFactory : _inner.GetService(serviceType);

        public object GetRequiredService(Type serviceType) =>
            serviceType == typeof(IServiceScopeFactory) ? ScopeFactory : _inner.GetRequiredService(serviceType);
    }
}
