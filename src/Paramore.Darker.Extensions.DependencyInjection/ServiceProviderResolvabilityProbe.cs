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
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.PipelineValidation;

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// Answers whether a component type is registered, from the container's registrations.
    /// It creates nothing: <see cref="IServiceProviderIsService"/> reads registrations, and reports a closed generic
    /// as registered when an open generic registration covers it.
    /// </summary>
    internal sealed class ServiceProviderResolvabilityProbe : IAmAComponentResolvabilityProbe
    {
        private readonly IServiceProviderIsService _isService;

        private ServiceProviderResolvabilityProbe(IServiceProviderIsService isService) => _isService = isService;

        /// <summary>
        /// Creates a probe over the provider's <see cref="IServiceProviderIsService"/>.
        /// </summary>
        /// <param name="provider">The built service provider.</param>
        /// <returns>The probe, or null when the container supplies no <see cref="IServiceProviderIsService"/>.</returns>
        public static ServiceProviderResolvabilityProbe? From(IServiceProvider provider)
        {
            var isService = provider.GetService<IServiceProviderIsService>();
            return isService is null ? null : new ServiceProviderResolvabilityProbe(isService);
        }

        /// <inheritdoc />
        public bool Resolves(Type type) => _isService.IsService(type);
    }
}
