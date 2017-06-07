#if NETSTANDARD
using System;
using System.Collections.Generic;

namespace Paramore.Darker.RemoteQueries
{
    public abstract class HttpRemoteQueryRegistry : IRemoteQueryRegistry
    {
        protected IDictionary<Type, Func<IQueryHandler>> HandlerFactories { get; } = new Dictionary<Type, Func<IQueryHandler>>();

        public bool CanHandle(Type query) => HandlerFactories.ContainsKey(query);

        public IQueryHandler ResolveHandler(Type query) => HandlerFactories[query]();
    }
}
#endif