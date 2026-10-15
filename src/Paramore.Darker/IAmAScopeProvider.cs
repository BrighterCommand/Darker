#nullable enable annotations
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

using System;

namespace Paramore.Darker
{
    /// <summary>
    /// Offers Darker the scope that is ambient for the current call, such as the scope of the web request
    /// that is executing a query. Darker may adopt the offered scope instead of creating its own.
    /// <para>
    /// Darker never disposes a scope returned from <see cref="GetAmbient"/>, so a provider must not create
    /// a scope that it expects Darker to dispose. Implementations must be safe to call concurrently.
    /// </para>
    /// </summary>
    public interface IAmAScopeProvider
    {
        /// <summary>
        /// Gets the scope that is ambient for the current call.
        /// </summary>
        /// <param name="affinity">The effective affinity. Darker asks only when it is <see cref="ScopeAffinity.JoinAmbient"/>.</param>
        /// <returns>The ambient scope, or null when none is ambient.</returns>
        IAmAScope? GetAmbient(ScopeAffinity affinity);
    }
}
