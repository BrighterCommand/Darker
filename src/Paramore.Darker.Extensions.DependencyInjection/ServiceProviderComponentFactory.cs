#region Licence
/* The MIT License (MIT)
Copyright © 2024 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */
#endregion

using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// Single DI-backed factory for both handlers and decorators, sync and async. Merging the
    /// formerly separate handler and decorator factories lets a Singleton dependency injected into
    /// both a handler and a decorator share one singleton cache, and lets handler and decorator
    /// read the same per-query child scope (owned by the <see cref="IAmALifetime"/>).
    /// </summary>
    internal sealed class ServiceProviderComponentFactory :
        IQueryHandlerFactory, IQueryHandlerFactoryAsync,
        IQueryHandlerDecoratorFactory, IQueryHandlerDecoratorFactoryAsync
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ServiceLifetime _handlerLifetime;

        // Keyed on the per-query lifetime token so all Create calls of one execution (handler and
        // its decorators) share a single child scope — without holding per-query state in a mutable
        // field on this shared singleton factory. Weak keys let the table entry be collected once the
        // token is gone; the scope itself is disposed by the token (via IAmALifetime.Add), not here.
        private readonly ConditionalWeakTable<IAmALifetime, ServiceProviderLifetimeScope> _scopes =
            new ConditionalWeakTable<IAmALifetime, ServiceProviderLifetimeScope>();

        public ServiceProviderComponentFactory(IServiceProvider serviceProvider, ServiceLifetime handlerLifetime)
        {
            _serviceProvider = serviceProvider;
            _handlerLifetime = handlerLifetime;
        }

        public IQueryHandler Create(Type handlerType, IAmALifetime lifetime)
        {
            return (IQueryHandler) Resolve(handlerType, lifetime);
        }

        public void Release(IQueryHandler handler, IAmALifetime lifetime)
        {
            // Scoped/Transient teardown is owned by the lifetime's child scope; Singletons are
            // never disposed by Darker. Nothing to do here.
        }

        public T Create<T>(Type decoratorType, IAmALifetime lifetime) where T : IQueryHandlerDecorator
        {
            return (T) Resolve(decoratorType, lifetime);
        }

        public void Release<T>(T handler, IAmALifetime lifetime) where T : IQueryHandlerDecorator
        {
            // Scoped/Transient teardown is owned by the lifetime's child scope; Singletons are
            // never disposed by Darker. Nothing to do here.
        }

        private object Resolve(Type componentType, IAmALifetime lifetime)
        {
            if (_handlerLifetime == ServiceLifetime.Singleton)
                return _serviceProvider.GetService(componentType);

            var scope = _scopes.GetValue(lifetime, CreateScope);
            return scope.Resolve(componentType);
        }

        private ServiceProviderLifetimeScope CreateScope(IAmALifetime lifetime)
        {
            var scope = new ServiceProviderLifetimeScope(_serviceProvider);
            lifetime.Add(scope);
            return scope;
        }
    }
}
