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

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// A copy of the facts of one service descriptor that the lifetime rules read.
    /// </summary>
    internal sealed class DescriptorRecord
    {
        private DescriptorRecord(Type serviceType, ServiceLifetime lifetime, bool isKeyed, Type? implementationType, object? implementationInstance)
        {
            ServiceType = serviceType;
            Lifetime = lifetime;
            IsKeyed = isKeyed;
            ImplementationType = implementationType;
            ImplementationInstance = implementationInstance;
        }

        public Type ServiceType { get; }

        public ServiceLifetime Lifetime { get; }

        public bool IsKeyed { get; }

        /// <summary>The implementation type, from the keyed or unkeyed property as the descriptor requires, or null for a factory or an instance.</summary>
        public Type? ImplementationType { get; }

        /// <summary>The implementation instance, from the keyed or unkeyed property as the descriptor requires, or null.</summary>
        public object? ImplementationInstance { get; }

        public static DescriptorRecord From(ServiceDescriptor descriptor) =>
            descriptor.IsKeyedService
                ? new DescriptorRecord(descriptor.ServiceType, descriptor.Lifetime, true, descriptor.KeyedImplementationType, descriptor.KeyedImplementationInstance)
                : new DescriptorRecord(descriptor.ServiceType, descriptor.Lifetime, false, descriptor.ImplementationType, descriptor.ImplementationInstance);
    }
}
