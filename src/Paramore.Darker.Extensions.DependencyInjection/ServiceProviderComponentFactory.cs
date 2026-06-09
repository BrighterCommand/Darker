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

        public ServiceProviderComponentFactory(IServiceProvider serviceProvider, ServiceLifetime handlerLifetime)
        {
            _serviceProvider = serviceProvider;
            _handlerLifetime = handlerLifetime;
        }

        public IQueryHandler Create(Type handlerType, IAmALifetime lifetime)
        {
            return (IQueryHandler) _serviceProvider.GetService(handlerType);
        }

        public void Release(IQueryHandler handler, IAmALifetime lifetime)
        {
            var disposal = handler as IDisposable;
            disposal?.Dispose();
        }

        public T Create<T>(Type decoratorType, IAmALifetime lifetime) where T : IQueryHandlerDecorator
        {
            return (T) _serviceProvider.GetService(decoratorType);
        }

        public void Release<T>(T handler, IAmALifetime lifetime) where T : IQueryHandlerDecorator
        {
            var disposal = handler as IDisposable;
            disposal?.Dispose();
        }
    }
}
