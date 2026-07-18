using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker;

namespace Paramore.Darker.Validation.Tests.TestDoubles;

/// <summary>
/// Concrete subclass of <see cref="ValidateQueryDecoratorAsync{TQuery,TResult}"/> whose
/// <c>ValidateAsync</c> implementation returns a caller-supplied collection of errors.
/// Used across async abstract-decorator tests to exercise both the valid (empty errors)
/// and invalid (non-empty errors) paths without a real validation provider.
/// </summary>
internal sealed class StubValidateQueryDecoratorAsync<TQuery, TResult> : ValidateQueryDecoratorAsync<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private readonly IReadOnlyCollection<QueryValidationError> _errors;

    public StubValidateQueryDecoratorAsync(IReadOnlyCollection<QueryValidationError> errors)
    {
        _errors = errors;
    }

    protected override Task<IReadOnlyCollection<QueryValidationError>> ValidateAsync(
        TQuery query, CancellationToken cancellationToken)
        => Task.FromResult(_errors);
}
