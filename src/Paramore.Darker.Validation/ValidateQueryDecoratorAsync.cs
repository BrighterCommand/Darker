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
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Validation;

/// <summary>
/// Abstract template-method decorator that validates a query asynchronously before passing
/// control to the next handler in the pipeline. When <see cref="ValidateAsync"/> returns one
/// or more errors the decorator throws <see cref="QueryValidationException"/> and
/// short-circuits the pipeline; when the query is valid it awaits <c>next</c> and returns
/// its result unchanged.
/// </summary>
/// <remarks>
/// Provider subclasses (e.g. FluentValidation, DataAnnotations) override
/// <see cref="ValidateAsync"/> to supply async validation logic. The attribute
/// <c>ValidateQueryAttributeAsync</c> names this abstract open-generic type; a
/// <c>Use*()</c> registration maps it to the concrete provider subclass so DI resolves
/// the right implementation at pipeline runtime.
/// </remarks>
/// <typeparam name="TQuery">The query type, constrained to <see cref="IQuery{TResult}"/>.</typeparam>
/// <typeparam name="TResult">The result type produced by the query.</typeparam>
public abstract class ValidateQueryDecoratorAsync<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <inheritdoc />
    public IQueryContext Context { get; set; } = null!;

    /// <inheritdoc />
    public void InitializeFromAttributeParams(object[] attributeParams) { }

    /// <inheritdoc />
    /// <remarks>
    /// Template method: awaits <see cref="ValidateAsync"/> first; throws
    /// <see cref="QueryValidationException"/> when errors are present; otherwise awaits
    /// <paramref name="next"/> and returns its result unchanged.
    /// </remarks>
    public async Task<TResult> ExecuteAsync(
        TQuery query,
        Func<TQuery, CancellationToken, Task<TResult>> next,
        Func<TQuery, CancellationToken, Task<TResult>> fallback,
        CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(query, cancellationToken).ConfigureAwait(false);
        if (errors.Count > 0)
            throw new QueryValidationException(errors);

        return await next(query, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates the supplied query asynchronously and returns any validation errors.
    /// Return an empty collection to indicate the query is valid.
    /// </summary>
    /// <param name="query">The query to validate.</param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> to observe for cancellation requests.
    /// </param>
    /// <returns>
    /// A task that resolves to a read-only collection of <see cref="QueryValidationError"/>
    /// instances; empty when the query passes validation.
    /// </returns>
    protected abstract Task<IReadOnlyCollection<QueryValidationError>> ValidateAsync(
        TQuery query, CancellationToken cancellationToken);
}
