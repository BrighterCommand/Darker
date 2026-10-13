#nullable enable annotations
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A handler factory that returns a fresh <see cref="RecordingScope"/> from each
    /// <see cref="CreatePipelineScope"/> call and records the lifetime handed to each <see cref="Create"/> call.
    /// </summary>
    internal sealed class ScopeSupplyingHandlerFactory : IQueryHandlerFactory, IQueryHandlerFactoryAsync
    {
        private readonly List<RecordingScope> _scopesCreated = new List<RecordingScope>();
        private readonly List<IAmALifetime> _lifetimesSeen = new List<IAmALifetime>();

        /// <summary>The scopes returned from <see cref="CreatePipelineScope"/>, in call order.</summary>
        public IReadOnlyList<RecordingScope> ScopesCreated => _scopesCreated;

        /// <summary>The lifetimes passed to <see cref="Create"/>, in call order.</summary>
        public IReadOnlyList<IAmALifetime> LifetimesSeen => _lifetimesSeen;

        public IAmAScope? CreatePipelineScope()
        {
            var scope = new RecordingScope();
            _scopesCreated.Add(scope);
            return scope;
        }

        public IQueryHandler Create(Type handlerType, IAmALifetime lifetime)
        {
            _lifetimesSeen.Add(lifetime);
            return (IQueryHandler)Activator.CreateInstance(handlerType)!;
        }

        public void Release(IQueryHandler handler, IAmALifetime lifetime)
        {
        }

        public ValueTask ReleaseAsync(IQueryHandler handler, IAmALifetime lifetime) => default;
    }
}
