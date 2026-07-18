using System;
using System.Collections.Generic;

namespace Paramore.Darker.Validation;

/// <summary>
/// Abstract template-method decorator that validates a query before passing control to the next
/// handler in the pipeline. When <see cref="Validate"/> returns one or more errors the decorator
/// throws <see cref="QueryValidationException"/> and short-circuits the pipeline; when the query
/// is valid it calls <c>next</c> and returns its result unchanged.
/// </summary>
/// <remarks>
/// Provider subclasses (e.g. FluentValidation, DataAnnotations) override <see cref="Validate"/>
/// to supply validation logic. The attribute <c>ValidateQueryAttribute</c> names this abstract
/// open-generic type; a <c>Use*()</c> registration maps it to the concrete provider subclass so DI
/// resolves the right implementation at pipeline runtime.
/// </remarks>
/// <typeparam name="TQuery">The query type, constrained to <see cref="IQuery{TResult}"/>.</typeparam>
/// <typeparam name="TResult">The result type produced by the query.</typeparam>
public abstract class ValidateQueryDecorator<TQuery, TResult> : IQueryHandlerDecorator<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <inheritdoc />
    public IQueryContext Context { get; set; } = null!;

    /// <inheritdoc />
    public void InitializeFromAttributeParams(object[] attributeParams) { }

    /// <inheritdoc />
    /// <remarks>
    /// Template method: calls <see cref="Validate"/> first; throws
    /// <see cref="QueryValidationException"/> when errors are present; otherwise delegates to
    /// <paramref name="next"/> and returns its result unchanged.
    /// </remarks>
    public TResult Execute(TQuery query, Func<TQuery, TResult> next, Func<TQuery, TResult> fallback)
    {
        var errors = Validate(query);
        if (errors.Count > 0)
            throw new QueryValidationException(errors);

        return next(query);
    }

    /// <summary>
    /// Validates the supplied query and returns any validation errors.
    /// Return an empty collection to indicate the query is valid.
    /// </summary>
    /// <param name="query">The query to validate.</param>
    /// <returns>
    /// A read-only collection of <see cref="QueryValidationError"/> instances;
    /// empty when the query passes validation.
    /// </returns>
    protected abstract IReadOnlyCollection<QueryValidationError> Validate(TQuery query);
}
