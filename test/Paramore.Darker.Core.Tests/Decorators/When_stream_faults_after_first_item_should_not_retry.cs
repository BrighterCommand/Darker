// Copyright (c) 2025, Ian Cooper
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the
// following conditions are met:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the
// Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies.Handlers;
using Polly;
using Polly.Registry;
using Polly.Retry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Decorators
{
    public class StreamResiliencePostFirstItemFaultTests
    {
        [Fact]
        public async Task When_stream_faults_after_first_item_should_propagate_without_retry_and_without_re_emitting_items()
        {
            // Arrange — retry pipeline configured to handle InvalidOperationException (but it must NOT
            // retry faults that occur after the first item has already been yielded)
            const string pipelineName = "Retry";
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(pipelineName, (builder, _) =>
                builder.AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.Zero
                }));

            var context = new QueryContext { ResiliencePipeline = registry };
            var decorator = new UseResiliencePipelineStreamHandler<FaultingStreamQuery, string>
            {
                Context = context
            };
            decorator.InitializeFromAttributeParams(new object[] { pipelineName });

            // FaultingStreamHandler yields ItemsBeforeFault items, then throws InvalidOperationException
            var query = new FaultingStreamQuery();
            var handler = new FaultingStreamHandler { Context = context };

            // Act — collect items until the mid-stream fault propagates
            var results = new List<string>();
            var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            {
                await foreach (var item in decorator.Execute(
                    query,
                    (q, ct) => handler.ExecuteAsync(q, ct),
                    default))
                {
                    results.Add(item);
                }
            });

            // Assert — items before the fault were observed; exception propagated; no re-emission
            // (if the pipeline had retried, results would contain the items repeated N times)
            results.Count.ShouldBe(FaultingStreamHandler.ItemsBeforeFault,
                "only items yielded before the fault should be present — no retry re-emission");
            exception.Message.ShouldBe(FaultingStreamHandler.ExceptionMessage,
                "the original exception propagates unwrapped");
        }
    }
}
