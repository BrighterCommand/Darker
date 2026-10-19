#nullable enable annotations
using System;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A handler factory whose <see cref="CreatePipelineScope"/> throws the supplied exception, and which
    /// counts any call to <see cref="Create"/>, <see cref="Release"/> or <see cref="ReleaseAsync"/>.
    /// </summary>
    internal sealed class ThrowingScopeHandlerFactory : IQueryHandlerFactory, IQueryHandlerFactoryAsync
    {
        private readonly Exception _fault;

        public ThrowingScopeHandlerFactory(Exception fault) => _fault = fault;

        public int CreateCount { get; private set; }

        public int ReleaseCount { get; private set; }

        public IAmAScope? CreatePipelineScope() => throw _fault;

        public IQueryHandler Create(Type handlerType, IAmALifetime lifetime)
        {
            CreateCount++;
            return (IQueryHandler)Activator.CreateInstance(handlerType)!;
        }

        public void Release(IQueryHandler handler, IAmALifetime lifetime) => ReleaseCount++;

        public ValueTask ReleaseAsync(IQueryHandler handler, IAmALifetime lifetime)
        {
            ReleaseCount++;
            return default;
        }
    }
}
