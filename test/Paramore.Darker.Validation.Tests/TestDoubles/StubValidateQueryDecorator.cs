using System.Collections.Generic;
using Paramore.Darker;

namespace Paramore.Darker.Validation.Tests.TestDoubles;

/// <summary>
/// Concrete subclass of <see cref="ValidateQueryDecorator{TQuery,TResult}"/> whose
/// <c>Validate</c> implementation returns a caller-supplied collection of errors.
/// Used across sync abstract-decorator tests to exercise both the valid (empty errors)
/// and invalid (non-empty errors) paths without a real validation provider.
/// </summary>
internal sealed class StubValidateQueryDecorator<TQuery, TResult> : ValidateQueryDecorator<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private readonly IReadOnlyCollection<QueryValidationError> _errors;

    public StubValidateQueryDecorator(IReadOnlyCollection<QueryValidationError> errors)
    {
        _errors = errors;
    }

    protected override IReadOnlyCollection<QueryValidationError> Validate(TQuery query) => _errors;
}
