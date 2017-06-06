using System;

namespace Paramore.Darker
{
    public sealed class NullRemoteQueryRegistry : IRemoteQueryRegistry
    {
        public bool CanHandle(Type query) => false;
        public IQueryHandler ResolveHandler(Type query) => throw new NotSupportedException();
    }
}