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
using Microsoft.Extensions.DependencyInjection;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// A copy of the service collection's descriptors, taken when ValidatePipelines is called, with the options AddDarker built.
    /// </summary>
    internal sealed class ServiceRegistrationSnapshot
    {
        private const string UNKNOWN_SCOPE_PROVIDER = "the registered scope provider";

        private readonly IReadOnlyList<DescriptorRecord> _descriptors;

        private ServiceRegistrationSnapshot(IReadOnlyList<DescriptorRecord> descriptors, DarkerOptions options)
        {
            _descriptors = descriptors;
            Options = options;
        }

        public DarkerOptions Options { get; }

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
                : DescribeScopeProvider(provider, providers.Count - 1);

            return new ScopeConfiguration(affinity, source, Options.HandlerLifetime, provider is not null,
                providers.Select(RegisterScopeProvider).ToList());
        }

        /// <summary>
        /// The handlers and decorators whose registrations <c>CaptiveDependency</c> examines, in registration order. Only the effective (last unkeyed) descriptor for a service type counts, and an alias registration (a service type other than its implementation type) is ignored.
        /// </summary>
        public IEnumerable<ComponentRegistration> Components =>
            _descriptors
                .Where(d => !d.IsKeyed && d.RuntimeType is not null && d.ServiceType == d.RuntimeType && ReferenceEquals(d, LastUnkeyedDescriptorOf(d.ServiceType)))
                .Select(d => (Descriptor: d, Kind: KindOf(d.RuntimeType!)))
                .Where(c => c.Kind is not null)
                .Select(c => new ComponentRegistration(c.Descriptor.RuntimeType!, c.Kind!, SingletonReasonOf(c.Descriptor)));

        private static string? KindOf(Type type) =>
            typeof(IQueryHandler).IsAssignableFrom(type) ? "Handler"
            : typeof(IQueryHandlerDecorator).IsAssignableFrom(type) ? "Decorator"
            : null;

        /// <summary>The last unkeyed descriptor registered for the service type, or null.</summary>
        public DescriptorRecord? LastUnkeyedDescriptorOf(Type serviceType) => UnkeyedDescriptorsOf(serviceType).LastOrDefault();

        /// <summary>The last keyed descriptor registered for the service type and key, or null.</summary>
        public DescriptorRecord? LastKeyedDescriptorOf(Type serviceType, object key) =>
            _descriptors.LastOrDefault(d => d.IsKeyed && d.ServiceType == serviceType && Equals(d.ServiceKey, key));

        private string? SingletonReasonOf(DescriptorRecord descriptor) =>
            Options.HandlerLifetime == ServiceLifetime.Singleton ? "HandlerLifetime is Singleton"
            : descriptor.Lifetime == ServiceLifetime.Singleton ? "its own registration is Singleton"
            : null;

        /// <summary>
        /// Renders an unkeyed <see cref="IAmAScopeProvider"/> descriptor: its implementation type, or for a factory
        /// "a factory registration at position N", where N is its 1-based position among the unkeyed provider descriptors.
        /// </summary>
        private static string DescribeScopeProvider(DescriptorRecord? provider, int index)
        {
            if (provider is null) return UNKNOWN_SCOPE_PROVIDER;
            if (provider.IsFactory) return $"a factory registration at position {index + 1}";

            return provider.RuntimeType?.Name ?? UNKNOWN_SCOPE_PROVIDER;
        }

        private static ScopeProviderRegistration RegisterScopeProvider(DescriptorRecord provider, int index)
        {
            var identity = provider.IsFactory
                ? (object)index
                : provider.RuntimeType ?? (object)provider;
            return new ScopeProviderRegistration(identity, DescribeScopeProvider(provider, index));
        }

        private IEnumerable<DescriptorRecord> UnkeyedDescriptorsOf(Type serviceType) =>
            _descriptors.Where(d => !d.IsKeyed && d.ServiceType == serviceType);
    }
}
