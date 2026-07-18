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
