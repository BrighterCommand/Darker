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
    /// One unkeyed <see cref="IAmAScopeProvider"/> registration: what it is, for comparing, and how to name it, for messages.
    /// </summary>
    /// <remarks>
    /// Two registrations are the same when their identity is: the implementation <see cref="Type"/>, or for a factory
    /// its position, because a factory carries no type. The description is display text and plays no part in the comparison.
    /// </remarks>
    internal sealed class ScopeProviderRegistration : IEquatable<ScopeProviderRegistration>
    {
        private const string UNKNOWN_SCOPE_PROVIDER = "the registered scope provider";

        private readonly object _identity;

        public ScopeProviderRegistration(object identity, string description)
        {
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        /// <summary>The registration rendered for a message.</summary>
        public string Description { get; }

        /// <summary>
        /// Creates the registration for an unkeyed <see cref="IAmAScopeProvider"/> descriptor. A factory registration is
        /// identified by its position, any other by its implementation type.
        /// </summary>
        /// <param name="provider">The descriptor.</param>
        /// <param name="index">The 0-based position of the descriptor among the unkeyed provider descriptors.</param>
        public static ScopeProviderRegistration From(DescriptorRecord provider, int index)
        {
            var identity = provider.IsFactory
                ? (object)index
                : provider.RuntimeType ?? (object)provider;
            return new ScopeProviderRegistration(identity, Describe(provider, index));
        }

        /// <summary>
        /// Renders an unkeyed <see cref="IAmAScopeProvider"/> descriptor: its implementation type, or for a factory
        /// "a factory registration at position N", where N is its 1-based position among the unkeyed provider descriptors.
        /// </summary>
        /// <param name="provider">The descriptor, or null when there is none.</param>
        /// <param name="index">The 0-based position of the descriptor among the unkeyed provider descriptors.</param>
        public static string Describe(DescriptorRecord? provider, int index)
        {
            if (provider is null) return UNKNOWN_SCOPE_PROVIDER;
            if (provider.IsFactory) return $"a factory registration at position {index + 1}";

            return provider.RuntimeType?.Name ?? UNKNOWN_SCOPE_PROVIDER;
        }

        public bool Equals(ScopeProviderRegistration? other) => other is not null && _identity.Equals(other._identity);

        public override bool Equals(object? obj) => Equals(obj as ScopeProviderRegistration);

        public override int GetHashCode() => _identity.GetHashCode();
    }
}
