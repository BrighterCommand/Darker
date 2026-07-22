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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Paramore.Darker.Extensions.DependencyInjection;

namespace Paramore.Darker.Caching;

/// <summary>
/// Extension methods for <see cref="IDarkerHandlerBuilder"/> that wire up the async caching
/// decorator by registering the open-generic <see cref="CacheableQueryDecoratorAsync{TQuery,TResult}"/>
/// and the default <see cref="ICacheKeyGenerator"/> in DI.
/// </summary>
public static class CachingDarkerBuilderExtensions
{
    /// <summary>
    /// Registers the async caching decorator and the default cache-key generator so that
    /// handlers annotated with <see cref="CacheableQueryAttributeAsync"/> are automatically
    /// wrapped by <see cref="CacheableQueryDecoratorAsync{TQuery,TResult}"/> in the pipeline.
    /// </summary>
    /// <param name="builder">The Darker handler builder. Must not be null.</param>
    /// <returns>The builder, for chaining.</returns>
    public static IDarkerHandlerBuilder AddCaching(this IDarkerHandlerBuilder builder)
    {
        builder.RegisterDecorator(typeof(CacheableQueryDecoratorAsync<,>));
        builder.Services.TryAddSingleton<ICacheKeyGenerator, DefaultCacheKeyGenerator>();
        return builder;
    }
}
