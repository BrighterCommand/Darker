#region Licence
/* The MIT License (MIT)
Copyright © 2026 Ian Cooper <ian_hammond_cooper@yahoo.co.uk>

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

#nullable enable annotations

namespace Paramore.Darker
{
    /// <summary>
    /// States whether a query may join an ambient scope or must have a scope of its own.
    /// </summary>
    /// <remarks>
    /// The effective affinity is the one registered with <c>AddDarkerScopeProvider</c> (or
    /// <c>AddDarkerRequestScope</c>) when there is one, and otherwise <c>DarkerOptions.DefaultScopeAffinity</c>.
    /// </remarks>
    public enum ScopeAffinity
    {
        /// <summary>
        /// Darker creates and owns a new scope for each query, and disposes it when the query completes.
        /// This is the default, and its underlying value is zero. Use it for queries that run concurrently
        /// within one ambient scope, such as under <c>Task.WhenAll</c>.
        /// </summary>
        AlwaysNew = 0,

        /// <summary>
        /// Darker adopts an offered ambient scope when it is allowed to, and otherwise creates its own.
        /// Darker never disposes an adopted scope.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Adoption requires a <c>HandlerLifetime</c> of <c>Scoped</c>. With any other handler lifetime Darker
        /// creates its own scope and warns once.
        /// </para>
        /// <para>
        /// Queries that join one ambient scope share its Scoped services, including a Scoped handler instance
        /// of the same handler type. Concurrent queries in one request (for example under <c>Task.WhenAll</c>)
        /// therefore need <see cref="AlwaysNew"/>. EF Core's <c>DbContext</c>, for one, rejects concurrent operations.
        /// </para>
        /// <para>
        /// For a streaming query, Darker reads the ambient scope on the first <c>MoveNextAsync</c>, not when the
        /// stream is created.
        /// </para>
        /// </remarks>
        JoinAmbient
    }
}
