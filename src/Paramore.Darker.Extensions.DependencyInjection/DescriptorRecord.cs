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
        private DescriptorRecord(Type serviceType, ServiceLifetime lifetime, object? serviceKey, Type? implementationType, object? implementationInstance, bool isFactory)
        {
            ServiceType = serviceType;
            Lifetime = lifetime;
            ServiceKey = serviceKey;
            ImplementationType = implementationType;
            ImplementationInstance = implementationInstance;
            IsFactory = isFactory;
        }

        public Type ServiceType { get; }

        public ServiceLifetime Lifetime { get; }

        public bool IsKeyed => ServiceKey is not null;

        /// <summary>The service key of a keyed descriptor, or null for an unkeyed one.</summary>
        public object? ServiceKey { get; }

        /// <summary>The implementation type, from the keyed or unkeyed property as the descriptor requires, or null for a factory or an instance.</summary>
        public Type? ImplementationType { get; }

        /// <summary>The implementation instance, from the keyed or unkeyed property as the descriptor requires, or null.</summary>
        public object? ImplementationInstance { get; }

        /// <summary>True when a factory registered the descriptor, so its implementation type is not known.</summary>
        public bool IsFactory { get; }

        /// <summary>The type that implements the service: the implementation type, or the instance's runtime type, or null for a factory or when neither is known.</summary>
        public Type? RuntimeType => IsFactory ? null : ImplementationType ?? ImplementationInstance?.GetType();

        public static DescriptorRecord From(ServiceDescriptor descriptor) =>
            descriptor.IsKeyedService
                ? new DescriptorRecord(descriptor.ServiceType, descriptor.Lifetime, descriptor.ServiceKey, descriptor.KeyedImplementationType, descriptor.KeyedImplementationInstance, descriptor.KeyedImplementationFactory is not null)
                : new DescriptorRecord(descriptor.ServiceType, descriptor.Lifetime, null, descriptor.ImplementationType, descriptor.ImplementationInstance, descriptor.ImplementationFactory is not null);
    }
}
