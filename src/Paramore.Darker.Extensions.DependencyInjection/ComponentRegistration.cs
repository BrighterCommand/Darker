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

namespace Paramore.Darker.Extensions.DependencyInjection
{
    /// <summary>
    /// A handler or decorator registration that <c>CaptiveDependency</c> examines: its type, what kind of component it is,
    /// and why it counts as Singleton (null when it does not).
    /// </summary>
    internal sealed class ComponentRegistration
    {
        public ComponentRegistration(Type implementationType, ComponentKind kind, SingletonReason? singletonReason)
        {
            ImplementationType = implementationType ?? throw new ArgumentNullException(nameof(implementationType));
            Kind = kind;
            SingletonReason = singletonReason;
        }

        /// <summary>The type the container constructs.</summary>
        public Type ImplementationType { get; }

        /// <summary>Whether the component is a handler or a decorator.</summary>
        public ComponentKind Kind { get; }

        /// <summary>Why the component counts as Singleton, or null when it does not.</summary>
        public SingletonReason? SingletonReason { get; }

        public override string ToString() => $"{Kind} '{ImplementationType.Name}'";
    }
}
