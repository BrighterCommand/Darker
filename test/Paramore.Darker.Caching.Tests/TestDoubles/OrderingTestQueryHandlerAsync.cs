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

using System.Threading;
using System.Threading.Tasks;

namespace Paramore.Darker.Caching.Tests.TestDoubles;

/// <summary>
/// Handler for <see cref="OrderingTestQuery"/> used in decorator-ordering / short-circuit tests.
/// Carries both the cache attribute (step = 1, lower step = outer position) and the recording
/// decorator attribute (step = 2, higher step = inner position). On a cache hit the cache
/// decorator is outermost and short-circuits <c>next</c>, so the recording decorator and this
/// handler must not be invoked.
/// </summary>
/// <remarks>
/// Step ordering recap: <c>PipelineBuilder.GetDecoratorsAsync</c> sorts attributes by step
/// <em>descending</em>, so the attribute with the highest step is processed first and sits closest
/// to the handler; the attribute with the lowest step is processed last and added outermost.
/// Therefore step = 1 (cache) is OUTER and step = 2 (recording) is INNER.
/// </remarks>
public sealed class OrderingTestQueryHandlerAsync
    : QueryHandlerAsync<OrderingTestQuery, OrderingTestQuery.Result>
{
    private readonly InnerInvocationRecorder _recorder;

    /// <summary>
    /// Initialises the handler with the shared invocation recorder.
    /// </summary>
    /// <param name="recorder">
    /// The singleton recorder shared with the test; receives a call to
    /// <see cref="InnerInvocationRecorder.IncrementHandler"/> each time the handler body runs.
    /// </param>
    public OrderingTestQueryHandlerAsync(InnerInvocationRecorder recorder)
    {
        _recorder = recorder;
    }

    /// <inheritdoc />
    [CacheableQueryAttributeAsync(1, 60)]
    [RecordingDecoratorAttributeAsync(2)]
    public override Task<OrderingTestQuery.Result> ExecuteAsync(
        OrderingTestQuery query,
        CancellationToken cancellationToken = default)
    {
        _recorder.IncrementHandler();
        return Task.FromResult(new OrderingTestQuery.Result { Value = query.Payload });
    }
}
