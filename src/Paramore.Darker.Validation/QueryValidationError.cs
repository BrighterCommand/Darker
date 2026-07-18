namespace Paramore.Darker.Validation;

/// <summary>
/// An information-holder record for a single validation failure on a query.
/// Mirrors Brighter's <c>RequestValidationError</c> for consistency across the two libraries.
/// </summary>
/// <param name="PropertyName">The name of the property that failed validation.</param>
/// <param name="ErrorMessage">A human-readable description of the failure.</param>
/// <param name="AttemptedValue">The value that was rejected, if available (null when the provider does not supply it).</param>
/// <param name="ErrorCode">A short machine-readable code for the failure, if available (null when the provider does not supply one, e.g. DataAnnotations).</param>
public sealed record QueryValidationError(
    string PropertyName,
    string ErrorMessage,
    object? AttemptedValue = null,
    string? ErrorCode = null);
