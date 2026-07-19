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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Validation;

namespace Paramore.Darker.Validation.DataAnnotations;

/// <summary>
/// A concrete <see cref="ValidateQueryDecoratorAsync{TQuery,TResult}"/> that validates query objects
/// using <see cref="System.ComponentModel.DataAnnotations.Validator"/>.
/// </summary>
/// <remarks>
/// Because <c>PipelineBuilder</c> closes every decorator over <c>typeof(IQuery&lt;TResult&gt;)</c>
/// at pipeline construction time, <typeparamref name="TQuery"/> is <c>IQuery&lt;TResult&gt;</c>
/// at runtime — not the concrete query type. The decorator calls
/// <see cref="System.ComponentModel.DataAnnotations.Validator.TryValidateObject"/> against the
/// runtime query object (via <see cref="DataAnnotationsValidationHelper"/>) so the concrete
/// type's DataAnnotations attributes are discovered.
/// DataAnnotations has no async validation API, so validation runs synchronously and the result
/// is wrapped in a completed task. No per-query validator registration is required — constraints
/// are declared as attributes on the query type (FR8). This provider does not fail-fast on a
/// "missing validator".
/// </remarks>
/// <typeparam name="TQuery">
/// The query type. At pipeline runtime this is <c>IQuery&lt;TResult&gt;</c>; the concrete
/// query type is recovered from the runtime object passed to <see cref="ValidateAsync"/>.
/// </typeparam>
/// <typeparam name="TResult">The result type produced by the query.</typeparam>
public class DataAnnotationsQueryValidatorDecoratorAsync<TQuery, TResult> : ValidateQueryDecoratorAsync<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    /// <summary>
    /// Runs <see cref="System.ComponentModel.DataAnnotations.Validator.TryValidateObject"/>
    /// synchronously against the runtime type of <paramref name="query"/> and wraps the result
    /// in a completed task. DataAnnotations has no async validation API.
    /// </summary>
    /// <param name="query">The query to validate.</param>
    /// <param name="cancellationToken">Not observed (DataAnnotations has no async API).</param>
    /// <returns>
    /// A completed task resolving to an empty collection when all DataAnnotations constraints
    /// on the concrete query type are satisfied; otherwise a collection of
    /// <see cref="QueryValidationError"/> instances describing each failure.
    /// </returns>
    protected override Task<IReadOnlyCollection<QueryValidationError>> ValidateAsync(
        TQuery query, CancellationToken cancellationToken)
        => Task.FromResult(DataAnnotationsValidationHelper.Validate(query!));
}
