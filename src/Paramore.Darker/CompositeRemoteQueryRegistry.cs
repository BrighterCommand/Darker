using System;
using System.Collections.Generic;
using System.Linq;

namespace Paramore.Darker
{
    public sealed class CompositeRemoteQueryRegistry : IRemoteQueryRegistry
    {
        private readonly IReadOnlyCollection<IRemoteQueryRegistry> _registries;

        public CompositeRemoteQueryRegistry(params IRemoteQueryRegistry[] registries)
        {
            _registries = registries;
        }

        public bool CanHandle(Type query) => _registries.Any(r => r.CanHandle(query));

        public IQueryHandler ResolveHandler(Type query) => _registries.First(r => r.CanHandle(query)).ResolveHandler(query);
    }
}