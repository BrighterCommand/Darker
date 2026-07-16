// The MIT License (MIT)
// Copyright (c) 2016 Ian Cooper
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// A <see cref="StreamQueryHandlerRegistry"/> that also registers each handler type
    /// in a <see cref="IServiceCollection"/> so the DI container can resolve it.
    /// </summary>
    internal sealed class ServiceCollectionStreamHandlerRegistry : StreamQueryHandlerRegistry
    {
        private readonly ServiceLifetime _lifetime;
        private readonly IServiceCollection _services;

        public ServiceCollectionStreamHandlerRegistry(IServiceCollection services, ServiceLifetime lifetime)
        {
            _services = services;
            _lifetime = lifetime;
        }

        /// <inheritdoc/>
        public override void Register(Type queryType, Type resultType, Type handlerType)
        {
            _services.TryAdd(new ServiceDescriptor(handlerType, handlerType, _lifetime));

            base.Register(queryType, resultType, handlerType);
        }

        /// <inheritdoc/>
        public override void Register<TQuery, TResult>(
            Func<TQuery, IQueryContext, Type?> router,
            params Type[] candidateHandlerTypes)
        {
            foreach (var candidate in candidateHandlerTypes)
                _services.TryAdd(new ServiceDescriptor(candidate, candidate, _lifetime));

            base.Register<TQuery, TResult>(router, candidateHandlerTypes);
        }
    }
}
