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

namespace Paramore.Darker
{
    /// <summary>
    /// A sync handler that can run its fallback for a query known only by its result type.
    /// </summary>
    /// <remarks>
    /// The pipeline knows <typeparamref name="TResult"/> but not the query type, so it cannot call
    /// <see cref="IQueryHandler{TQuery, TResult}.Fallback"/> directly. <see cref="QueryHandler{TQuery, TResult}"/>
    /// implements this role so the pipeline calls its fallback without reflection, which also keeps the
    /// fallback when the app is trimmed or compiled with Native AOT.
    /// </remarks>
    /// <typeparam name="TResult">The type of the query's result.</typeparam>
    internal interface IAmAFallbackTarget<TResult>
    {
        /// <summary>Runs the handler's fallback for <paramref name="query"/>.</summary>
        /// <param name="query">The query whose execution failed.</param>
        /// <returns>The fallback's result.</returns>
        TResult Fallback(IQuery<TResult> query);
    }
}
