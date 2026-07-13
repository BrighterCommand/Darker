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
    public class StreamResilienceRetryBeforeFirstItemTests
    {
        [Fact]
        public async Task When_establishment_fails_before_first_item_should_retry_fresh_stream_with_no_duplicate_emission()
        {
            // Arrange — retry pipeline that handles the transient InvalidOperationException
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
            var decorator = new UseResiliencePipelineStreamHandler<MultiItemStreamQuery, string>
            {
                Context = context
            };
            decorator.InitializeFromAttributeParams(new object[] { pipelineName });

            // Handler fails on its first attempt (throws before yielding any item), then succeeds
            var handler = new TransientlyFailingStreamHandler(failuresBeforeSuccess: 1);
            var query = new MultiItemStreamQuery();

            // Act — the Establish callback disposes the failed enumerator and retries with a fresh one
            var results = new List<string>();
            await foreach (var item in decorator.Execute(
                query,
                (q, ct) => handler.ExecuteAsync(q, ct),
                default))
            {
                results.Add(item);
            }

            // Assert — full sequence emitted exactly once (retry starts a fresh enumerable, no duplicates)
            results.ShouldBe(MultiItemStreamHandler.Items);
            handler.Calls.ShouldBe(2, "handler body ran twice: once for the failure, once for the successful retry");
        }
    }
}
