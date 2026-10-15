#nullable enable annotations
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
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Exceptions;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// Single DI-backed factory for both handlers and decorators, sync and async. Merging the
    /// formerly separate handler and decorator factories lets a Singleton dependency injected into
    /// both a handler and a decorator share one singleton cache, and lets handler and decorator
    /// read the same per-query child scope. That scope is created up front by
    /// <see cref="CreatePipelineScope"/> and reaches every <c>Create</c> call as
    /// <see cref="IAmALifetime.PipelineScope"/>, so the factory holds no per-query state.
    /// </summary>
    internal sealed class ServiceProviderComponentFactory(
        IServiceProvider serviceProvider,
        ServiceLifetime handlerLifetime,
        IAmAScopeProvider? scopeProvider,
        ScopeAffinity affinity,
        AdoptionDiagnostics adoptionDiagnostics)
        :
            IQueryHandlerFactory, IQueryHandlerFactoryAsync,
            IQueryHandlerDecoratorFactory, IQueryHandlerDecoratorFactoryAsync
    {
        // Rows 1 to 4 of the decision ladder (ADR 0024) depend only on configuration, so they are
        // evaluated once, when the factory is built.
        private readonly bool _adoptionEnabled = EvaluateAdoption(handlerLifetime, scopeProvider, affinity, adoptionDiagnostics);

        public IQueryHandler Create(Type handlerType, IAmALifetime lifetime)
        {
            return (IQueryHandler) Resolve(handlerType, lifetime);
        }

        // Singletons resolve from the captured provider and Darker never disposes them, so they
        // need no per-query scope; Scoped and Transient components share one child scope per query.
        public IAmAScope? CreatePipelineScope()
            => handlerLifetime == ServiceLifetime.Singleton ? null : new ServiceProviderLifetimeScope(serviceProvider);

        public void Release(IQueryHandler handler, IAmALifetime lifetime)
        {
            // Scoped/Transient teardown is owned by the pipeline scope; Singletons are
            // never disposed by Darker. Nothing to do here.
        }

        public ValueTask ReleaseAsync(IQueryHandler handler, IAmALifetime lifetime) => default;

        public T Create<T>(Type decoratorType, IAmALifetime lifetime) where T : IQueryHandlerDecorator
        {
            return (T) Resolve(decoratorType, lifetime);
        }

        public void Release<T>(T handler, IAmALifetime lifetime) where T : IQueryHandlerDecorator
        {
            // Scoped/Transient teardown is owned by the pipeline scope; Singletons are
            // never disposed by Darker. Nothing to do here.
        }

        public ValueTask ReleaseAsync<T>(T handler, IAmALifetime lifetime) where T : IQueryHandlerDecorator => default;

        private static bool EvaluateAdoption(
            ServiceLifetime handlerLifetime,
            IAmAScopeProvider? scopeProvider,
            ScopeAffinity affinity,
            AdoptionDiagnostics adoptionDiagnostics)
        {
            if (affinity != ScopeAffinity.JoinAmbient)
                return false;

            if (handlerLifetime != ServiceLifetime.Scoped)
            {
                adoptionDiagnostics.WarnOnce(
                    "scoped-handler-lifetime",
                    $"The scope affinity is JoinAmbient, but the handler lifetime is {handlerLifetime}. " +
                    "Adopting an ambient scope needs a Scoped handler lifetime, so each query gets a scope of its own.");
                return false;
            }

            if (scopeProvider == null)
            {
                adoptionDiagnostics.WarnOnce(
                    "no-scope-provider",
                    "The scope affinity is JoinAmbient, but no scope provider is registered, " +
                    "so each query gets a scope of its own. Register one with AddDarkerScopeProvider.");
                return false;
            }

            return true;
        }

        private object Resolve(Type componentType, IAmALifetime lifetime)
        {
            if (handlerLifetime == ServiceLifetime.Singleton)
                return serviceProvider.GetService(componentType);

            return RecognisedScope(componentType, lifetime).Services.GetService(componentType);
        }

        // Fails fast rather than resolving from the root provider, which would leak Scoped and
        // Transient components for the life of the container.
        private IAmAServiceProviderScope RecognisedScope(Type componentType, IAmALifetime lifetime)
        {
            var handle = lifetime?.PipelineScope;

            if (handle == null)
                throw new ConfigurationException(
                    $"Cannot resolve {componentType} with a {handlerLifetime} handler lifetime: the pipeline has no scope. " +
                    "Use the Darker handler factory that created the pipeline's scope.");

            if (handle is not IAmAServiceProviderScope scope)
                throw new ConfigurationException(
                    $"Cannot resolve {componentType} with a {handlerLifetime} handler lifetime: " +
                    $"the pipeline scope {handle.GetType()} was not created by this factory.");

            return scope;
        }
    }
}
