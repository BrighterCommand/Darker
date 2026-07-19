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
using System.Collections.Generic;

namespace Paramore.Darker.Validation;

/// <summary>
/// Thrown when a query fails validation. Carries the full collection of validation failures so callers
/// (e.g. ASP.NET Core middleware) can translate them into a structured error response (HTTP 400).
/// Mirrors Brighter's <c>RequestValidationException</c> for consistency across the two libraries.
/// </summary>
public sealed class QueryValidationException : Exception
{
    /// <summary>
    /// The provider-agnostic validation errors that caused the exception.
    /// </summary>
    public IReadOnlyCollection<QueryValidationError> Errors { get; }

    /// <summary>
    /// Initialises a new <see cref="QueryValidationException"/> with the supplied validation errors.
    /// </summary>
    /// <param name="errors">The collection of validation failures; must not be null.</param>
    public QueryValidationException(IReadOnlyCollection<QueryValidationError> errors)
        : base($"Query validation failed with {errors.Count} error(s).")
    {
        Errors = errors;
    }
}
