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
            var provider = UnkeyedDescriptorsOf(typeof(IAmAScopeProvider)).LastOrDefault();

            var affinity = registration?.Affinity ?? Options.DefaultScopeAffinity;
            var source = registration is null
                ? "DarkerOptions.DefaultScopeAffinity"
                : DescribeScopeProvider(provider);

            return new ScopeConfiguration(affinity, source, Options.HandlerLifetime, provider is not null);
        }

        /// <summary>
        /// Renders an unkeyed <see cref="IAmAScopeProvider"/> descriptor: its implementation type, or for a factory
        /// "a factory registration at position N", where N is its 1-based position among the unkeyed provider descriptors.
        /// </summary>
        private string DescribeScopeProvider(DescriptorRecord? provider)
        {
            if (provider is null) return "the registered scope provider";
            if (provider.IsFactory)
            {
                var position = UnkeyedDescriptorsOf(typeof(IAmAScopeProvider)).ToList().IndexOf(provider) + 1;
                return $"a factory registration at position {position}";
            }

            return provider.ImplementationType?.Name ?? provider.ImplementationInstance?.GetType().Name ?? "the registered scope provider";
        }

        private IEnumerable<DescriptorRecord> UnkeyedDescriptorsOf(Type serviceType) =>
            _descriptors.Where(d => !d.IsKeyed && d.ServiceType == serviceType);
    }
}
