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
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Validation;

namespace Paramore.Darker.Validation.DataAnnotations;

/// <summary>
/// Extension methods for <see cref="IDarkerHandlerBuilder"/> that wire up the DataAnnotations
/// validation provider by registering the abstract→concrete open-generic decorator mappings in DI.
/// </summary>
public static class DataAnnotationsDarkerBuilderExtensions
{
    /// <summary>
    /// Registers the DataAnnotations validation provider by adding open-generic
    /// <see cref="ServiceDescriptor"/>s that map the abstract core decorators
    /// (<see cref="ValidateQueryDecorator{TQuery,TResult}"/> and
    /// <see cref="ValidateQueryDecoratorAsync{TQuery,TResult}"/>) to their concrete
    /// DataAnnotations implementations so DI can resolve the concrete decorator when
    /// the pipeline asks for the abstract type.
    /// </summary>
    /// <param name="builder">The Darker handler builder. Must not be null.</param>
    /// <returns>The builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> is null.
    /// </exception>
    public static IDarkerHandlerBuilder UseDataAnnotations(this IDarkerHandlerBuilder builder)
    {
        if (builder is null)
            throw new ArgumentNullException(nameof(builder));

        builder.Services.Add(new ServiceDescriptor(
            typeof(ValidateQueryDecorator<,>),
            typeof(DataAnnotationsQueryValidatorDecorator<,>),
            ServiceLifetime.Transient));

        builder.Services.Add(new ServiceDescriptor(
            typeof(ValidateQueryDecoratorAsync<,>),
            typeof(DataAnnotationsQueryValidatorDecoratorAsync<,>),
            ServiceLifetime.Transient));

        return builder;
    }
}
