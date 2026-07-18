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

namespace Paramore.Darker.Validation;

/// <summary>
/// Marks a handler's async execute method for query validation. When present,
/// <see cref="GetDecoratorType"/> returns the abstract open generic
/// <see cref="ValidateQueryDecoratorAsync{TQuery,TResult}"/> so the pipeline can resolve the
/// concrete provider decorator configured via a <c>Use*()</c> registration call.
/// </summary>
/// <remarks>
/// Place this attribute on the handler's <c>ExecuteAsync</c> method and supply a <paramref name="step"/>
/// to control where validation runs in the decorator pipeline (higher step executes first).
/// A provider package (e.g. <c>Paramore.Darker.Validation.FluentValidation</c>) must be registered
/// via its <c>Use*()</c> extension; otherwise the pipeline cannot resolve the abstract decorator.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class ValidateQueryAttributeAsync : QueryHandlerAttributeAsync
{
    /// <summary>
    /// Initialises a new instance of <see cref="ValidateQueryAttributeAsync"/>.
    /// </summary>
    /// <param name="step">
    /// The step order for this decorator in the handler pipeline. Higher values execute earlier.
    /// </param>
    public ValidateQueryAttributeAsync(int step) : base(step) { }

    /// <inheritdoc />
    /// <returns>
    /// The abstract open generic <see cref="ValidateQueryDecoratorAsync{TQuery,TResult}"/>; DI maps
    /// this to the concrete provider subclass registered via a <c>Use*()</c> call.
    /// </returns>
    public override Type GetDecoratorType() => typeof(ValidateQueryDecoratorAsync<,>);

    /// <inheritdoc />
    /// <returns>An empty <see cref="object"/> array; validation carries no per-attribute state beyond <see cref="QueryHandlerAttributeAsync.Step"/>.</returns>
    public override object[] GetAttributeParams() => Array.Empty<object>();
}
