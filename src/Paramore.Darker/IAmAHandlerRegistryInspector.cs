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

#nullable enable

using System.Collections.Generic;

namespace Paramore.Darker
{
    /// <summary>
    /// Lets a handler registry report what is registered in it. Registries are otherwise only queried by query type,
    /// so without this role nothing can enumerate them.
    /// </summary>
    /// <remarks>
    /// Implementing it is optional and additive: <see cref="IQueryHandlerRegistry"/> and its siblings are unchanged.
    /// Implementations should not throw.
    /// </remarks>
    public interface IAmAHandlerRegistryInspector
    {
        /// <summary>Gets every registration, in registration order.</summary>
        /// <returns>The registrations; empty when nothing is registered.</returns>
        IReadOnlyList<HandlerRegistration> GetRegistrations();
    }
}
