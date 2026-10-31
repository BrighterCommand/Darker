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
        private readonly object _identity;

        public ScopeProviderRegistration(object identity, string description)
        {
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        /// <summary>The registration rendered for a message.</summary>
        public string Description { get; }

        public bool Equals(ScopeProviderRegistration? other) => other is not null && _identity.Equals(other._identity);

        public override bool Equals(object? obj) => Equals(obj as ScopeProviderRegistration);

        public override int GetHashCode() => _identity.GetHashCode();
    }
}
