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
    /// The outcome of checking a handler method for another path's decorator attributes: the message of the first
    /// wrong kind found with the types of the attributes of that kind, or null when there is none, or the failure that
    /// stopped the attributes being read.
    /// </summary>
    internal sealed class WrongKindCheck(string? message, IReadOnlyList<Type> attributeTypes, AttributeReadFailure? failure)
        : ReadOutcome(failure)
    {
        public string? Message { get; } = message;

        /// <summary>The types of the attributes of the first wrong kind found; empty when there is none.</summary>
        public IReadOnlyList<Type> AttributeTypes { get; } = attributeTypes;
    }
}
