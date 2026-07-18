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
