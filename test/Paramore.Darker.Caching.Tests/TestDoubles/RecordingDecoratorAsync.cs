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
using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Caching.Tests.TestDoubles;

/// <summary>
/// A minimal async pipeline decorator used in ordering / short-circuit tests. Each time its
/// <see cref="ExecuteAsync"/> is entered it increments <see cref="InnerInvocationRecorder.InnerDecoratorCallCount"/>
/// then delegates to <paramref name="next"/>. Because it is registered at a higher <c>step</c>
/// than <see cref="CacheableQueryAttributeAsync"/> (i.e. it sits <em>inside</em> the cache
/// decorator in the pipeline), it must not be invoked on a cache hit.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
public sealed class RecordingDecoratorAsync<TQuery, TResult> : IQueryHandlerDecoratorAsync<TQuery, TResult>
    where TQuery : IQuery<TResult>
{
    private readonly InnerInvocationRecorder _recorder;

    /// <inheritdoc />
    public IQueryContext Context { get; set; } = null!;

    /// <summary>
    /// Initialises the decorator with the shared invocation recorder.
    /// </summary>
    /// <param name="recorder">
    /// The singleton recorder shared with the test; receives a call to
    /// <see cref="InnerInvocationRecorder.IncrementInnerDecorator"/> each time this decorator runs.
    /// </param>
    public RecordingDecoratorAsync(InnerInvocationRecorder recorder)
    {
        _recorder = recorder;
    }

    /// <inheritdoc />
    public void InitializeFromAttributeParams(object[] attributeParams)
    {
        // No attribute state to apply.
    }

    /// <inheritdoc />
    public async Task<TResult> ExecuteAsync(
        TQuery query,
        Func<TQuery, CancellationToken, Task<TResult>> next,
        Func<TQuery, CancellationToken, Task<TResult>> fallback,
        CancellationToken cancellationToken = default)
    {
        _recorder.IncrementInnerDecorator();
        return await next(query, cancellationToken).ConfigureAwait(false);
    }
}
