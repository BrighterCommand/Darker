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
using System.Linq;
using FluentValidation;
using Paramore.Darker.Exceptions;
using Paramore.Darker.Validation;

namespace Paramore.Darker.Validation.FluentValidation;

/// <summary>
/// A concrete <see cref="ValidateQueryDecorator{TQuery,TResult}"/> that delegates validation
/// to a FluentValidation <see cref="IValidator{T}"/> resolved from an
/// <see cref="IServiceProvider"/> at validation time.
/// </summary>
/// <remarks>
/// Because <c>PipelineBuilder</c> closes every decorator over <c>typeof(IQuery&lt;TResult&gt;)</c>
/// at pipeline construction time, <typeparamref name="TQuery"/> is <c>IQuery&lt;TResult&gt;</c>
/// at runtime — not the concrete query type. The decorator therefore resolves the validator
/// using the <em>runtime</em> type of the query object:
/// <c>serviceProvider.GetService(typeof(IValidator&lt;&gt;).MakeGenericType(query.GetType()))</c>.
/// The non-generic <see cref="IValidator.Validate(IValidationContext)"/> is used to invoke the
/// validator since the compile-time type is not the concrete query type.
/// </remarks>
/// <typeparam name="TQuery">
/// The query type. At pipeline runtime this is <c>IQuery&lt;TResult&gt;</c>; the concrete
/// query type is recovered from <c>query.GetType()</c> inside <see cref="Validate"/>.
/// </typeparam>
/// <typeparam name="TResult">The result type produced by the query.</typeparam>
public class FluentValidationQueryValidatorDecorator<TQuery, TResult> : ValidateQueryDecorator<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initialises a new instance of
    /// <see cref="FluentValidationQueryValidatorDecorator{TQuery,TResult}"/>.
    /// </summary>
    /// <param name="serviceProvider">
    /// The <see cref="IServiceProvider"/> used to resolve
    /// <see cref="IValidator{T}"/> instances at validation time.
    /// </param>
    public FluentValidationQueryValidatorDecorator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Resolves a FluentValidation <see cref="IValidator{T}"/> for the runtime type of
    /// <paramref name="query"/>, runs synchronous validation, and returns any failures
    /// mapped to <see cref="QueryValidationError"/> instances.
    /// </summary>
    /// <param name="query">The query to validate.</param>
    /// <returns>
    /// An empty collection when validation succeeds; otherwise a collection of
    /// <see cref="QueryValidationError"/> instances describing each failure.
    /// </returns>
    /// <exception cref="ConfigurationException">
    /// Thrown when no <see cref="IValidator{T}"/> is registered for the runtime query type.
    /// A missing validator on a handler annotated with <c>[ValidateQuery]</c> is a
    /// configuration error (FR9 — fail-fast), not a silent no-op.
    /// </exception>
    protected override IReadOnlyCollection<QueryValidationError> Validate(TQuery query)
    {
        var validatorType = typeof(IValidator<>).MakeGenericType(query.GetType());
        var validator = (IValidator?)_serviceProvider.GetService(validatorType);

        if (validator == null)
            throw new ConfigurationException(
                $"No FluentValidation IValidator<{query.GetType().Name}> is registered. " +
                $"Register a validator for {query.GetType().Name} or remove [ValidateQuery] from the handler.");

        var validationResult = validator.Validate(new ValidationContext<object>(query));

        if (validationResult.IsValid)
            return Array.Empty<QueryValidationError>();

        return validationResult.Errors
            .Select(failure => new QueryValidationError(
                failure.PropertyName,
                failure.ErrorMessage,
                failure.AttemptedValue,
                failure.ErrorCode))
            .ToArray();
    }
}
