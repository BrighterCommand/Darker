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

using System;
using System.Collections.Generic;

namespace Paramore.Darker
{
    /// <summary>
    /// One registration in a handler registry: a query, its result type, and the handler types that can serve it.
    /// </summary>
    /// <param name="QueryType">The registered query type.</param>
    /// <param name="ResultType">The query's result type.</param>
    /// <param name="CandidateHandlerTypes">
    /// The handler types the query can be dispatched to, in registration order. A fixed registration has one;
    /// a routed registration has one per candidate. An entry is null when a null handler type was registered.
    /// </param>
    /// <remarks>The constructor does not validate its arguments; the registry that records a registration has already accepted it.</remarks>
    public sealed record HandlerRegistration(Type QueryType, Type ResultType, IReadOnlyList<Type?> CandidateHandlerTypes);
}
