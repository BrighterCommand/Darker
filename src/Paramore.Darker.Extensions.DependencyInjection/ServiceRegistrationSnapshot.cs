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

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// A copy of the service collection's descriptors, taken when ValidatePipelines is called, with the options AddDarker built.
    /// </summary>
    internal sealed class ServiceRegistrationSnapshot
    {
        private readonly IReadOnlyList<DescriptorRecord> _descriptors;

        private ServiceRegistrationSnapshot(IReadOnlyList<DescriptorRecord> descriptors, DarkerOptions options)
        {
            _descriptors = descriptors;
            Options = options;
        }

        private DarkerOptions Options { get; }

        public static ServiceRegistrationSnapshot Capture(IServiceCollection services, DarkerOptions options)
        {
            if (services is null) throw new ArgumentNullException(nameof(services));
            if (options is null) throw new ArgumentNullException(nameof(options));

            return new ServiceRegistrationSnapshot(services.Select(DescriptorRecord.From).ToList(), options);
        }

        /// <summary>
        /// Reads the scope configuration. The effective affinity is the last unkeyed <see cref="ScopeAffinityRegistration"/> instance,
        /// otherwise <see cref="DarkerOptions.DefaultScopeAffinity"/>.
        /// </summary>
        public ScopeConfiguration ReadScopeConfiguration()
        {
            var registration = UnkeyedDescriptorsOf(typeof(ScopeAffinityRegistration))
                .LastOrDefault()
                ?.ImplementationInstance as ScopeAffinityRegistration;
            var providers = UnkeyedDescriptorsOf(typeof(IAmAScopeProvider)).ToList();
            var provider = providers.LastOrDefault();

            var affinity = registration?.Affinity ?? Options.DefaultScopeAffinity;
            var source = registration is null
                ? "DarkerOptions.DefaultScopeAffinity"
                : ScopeProviderRegistration.Describe(provider, providers.Count - 1);

            return new ScopeConfiguration(affinity, source, Options.HandlerLifetime,
                providers.Select(ScopeProviderRegistration.From).ToList());
        }

        /// <summary>
        /// The handlers and decorators whose registrations <c>CaptiveDependency</c> examines, in registration order.
        /// </summary>
        public IEnumerable<ComponentRegistration> Components => _descriptors.Select(ComponentOf).OfType<ComponentRegistration>();

        /// <summary>
        /// A candidate is an unkeyed handler or decorator that registers itself (a service type equal to its implementation type,
        /// so not an alias) and is the effective, last unkeyed descriptor for that type. Factories and instances have no implementation type.
        /// Returns null for any other descriptor.
        /// </summary>
        private ComponentRegistration? ComponentOf(DescriptorRecord descriptor)
        {
            if (descriptor.IsKeyed
                || descriptor.ImplementationType is not { } type
                || descriptor.ServiceType != type
                || !ReferenceEquals(descriptor, LastUnkeyedDescriptorOf(descriptor.ServiceType)))
                return null;

            return KindOf(type) is { } kind
                ? new ComponentRegistration(type, kind, SingletonReasonOf(descriptor))
                : null;
        }

        private static ComponentKind? KindOf(Type type) =>
            typeof(IQueryHandler).IsAssignableFrom(type) ? ComponentKind.Handler
            : typeof(IQueryHandlerDecorator).IsAssignableFrom(type) ? ComponentKind.Decorator
            : null;

        /// <summary>The last unkeyed descriptor registered for the service type, or null.</summary>
        private DescriptorRecord? LastUnkeyedDescriptorOf(Type serviceType) => UnkeyedDescriptorsOf(serviceType).LastOrDefault();

        /// <summary>
        /// The descriptor the container would use for a constructor parameter: the last keyed descriptor for a
        /// <c>[FromKeyedServices]</c> parameter with a key, otherwise the last unkeyed descriptor for its type,
        /// falling back to the generic type definition of a constructed generic. Null when none matches.
        /// </summary>
        public DescriptorRecord? EffectiveDescriptorOf(ParameterInfo parameter)
        {
            var key = parameter.GetCustomAttribute<FromKeyedServicesAttribute>()?.Key;
            if (key is not null) return LastKeyedDescriptorOf(parameter.ParameterType, key);

            var parameterType = parameter.ParameterType;
            return LastUnkeyedDescriptorOf(parameterType)
                ?? (parameterType.IsConstructedGenericType ? LastUnkeyedDescriptorOf(parameterType.GetGenericTypeDefinition()) : null);
        }

        /// <summary>The last keyed descriptor registered for the service type and key, or null.</summary>
        private DescriptorRecord? LastKeyedDescriptorOf(Type serviceType, object key) =>
            _descriptors.LastOrDefault(d => d.IsKeyed && d.ServiceType == serviceType && Equals(d.ServiceKey, key));

        private SingletonReason? SingletonReasonOf(DescriptorRecord descriptor) =>
            Options.HandlerLifetime == ServiceLifetime.Singleton ? SingletonReason.HandlerLifetime
            : descriptor.Lifetime == ServiceLifetime.Singleton ? SingletonReason.OwnRegistration
            : null;

        private IEnumerable<DescriptorRecord> UnkeyedDescriptorsOf(Type serviceType) =>
            _descriptors.Where(d => !d.IsKeyed && d.ServiceType == serviceType);
    }
}
