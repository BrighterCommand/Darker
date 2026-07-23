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

namespace Paramore.Darker.Caching;

/// <summary>
/// Options for configuring the Darker caching integration via
/// <see cref="CachingDarkerBuilderExtensions.AddCaching(Paramore.Darker.Extensions.DependencyInjection.IDarkerHandlerBuilder,System.Action{CachingOptions})"/>.
/// </summary>
public sealed class CachingOptions
{
    /// <summary>
    /// Gets or sets a custom <see cref="ICacheKeyGenerator"/> that replaces the default
    /// <see cref="DefaultCacheKeyGenerator"/> for computing cache keys.
    /// When <see langword="null"/>, the default <see cref="DefaultCacheKeyGenerator"/> is
    /// registered instead.
    /// </summary>
    public ICacheKeyGenerator? KeyGenerator { get; set; }
}
