using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Paramore.Darker.Core.Tests.TestDoubles
{
    /// <summary>
    /// A decorator factory that creates decorators by type and records the lifetime handed to each
    /// <see cref="Create{T}"/> call. <see cref="SimpleHandlerDecoratorFactory"/> cannot do this, because
    /// its delegate never sees the lifetime.
    /// </summary>
    internal sealed class LifetimeRecordingDecoratorFactory : IQueryHandlerDecoratorFactory, IQueryHandlerDecoratorFactoryAsync
    {
        private readonly TeardownEventLog _log;
        private readonly List<IAmALifetime> _lifetimesSeen = new List<IAmALifetime>();

        public LifetimeRecordingDecoratorFactory(TeardownEventLog log = null) => _log = log;

        /// <summary>The lifetimes passed to <see cref="Create{T}"/>, in call order.</summary>
        public IReadOnlyList<IAmALifetime> LifetimesSeen => _lifetimesSeen;

        public T Create<T>(Type decoratorType, IAmALifetime lifetime) where T : IQueryHandlerDecorator
        {
            _lifetimesSeen.Add(lifetime);
            return (T)Activator.CreateInstance(decoratorType)!;
        }

        public void Release<T>(T handler, IAmALifetime lifetime) where T : IQueryHandlerDecorator
            => _log?.Record(TeardownEventLog.DECORATOR_RELEASED);

        public ValueTask ReleaseAsync<T>(T handler, IAmALifetime lifetime) where T : IQueryHandlerDecorator
        {
            _log?.Record(TeardownEventLog.DECORATOR_RELEASED);
            return default;
        }
    }
}
