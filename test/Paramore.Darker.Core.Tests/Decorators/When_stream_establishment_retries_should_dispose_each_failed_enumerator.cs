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
    public class StreamEstablishmentRetryDisposalTests
    {
        [Fact]
        public async Task When_establishment_retries_N_times_should_dispose_the_enumerator_from_each_failed_attempt()
        {
            // Arrange — retry pipeline with 2 retries (3 total attempts), all of which fail
            const int maxRetryAttempts = 2;
            const int expectedDisposals = maxRetryAttempts + 1; // original attempt + retries
            const string pipelineName = "Retry";

            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(pipelineName, (builder, _) =>
                builder.AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<InvalidOperationException>(),
                    MaxRetryAttempts = maxRetryAttempts,
                    Delay = TimeSpan.Zero
                }));

            var context = new QueryContext { ResiliencePipeline = registry };
            var decorator = new UseResiliencePipelineStreamHandler<MultiItemStreamQuery, string>
            {
                Context = context
            };
            decorator.InitializeFromAttributeParams(new object[] { pipelineName });

            // Handler always fails before yielding any item — every establishment attempt throws
            var handler = new TransientlyFailingStreamHandler(failuresBeforeSuccess: int.MaxValue);
            var query = new MultiItemStreamQuery();
            var disposeCount = 0;

            // Act — all attempts fail; each failed enumerator must be disposed inside Establish's catch
            await Should.ThrowAsync<InvalidOperationException>(async () =>
            {
                await foreach (var _ in decorator.Execute(
                    query,
                    (q, ct) => new DisposalCountingEnumerable<string>(handler.ExecuteAsync(q, ct), () => disposeCount++),
                    default))
                {
                }
            });

            // Assert — one dispose per failed attempt: no enumerator is abandoned on retry
            disposeCount.ShouldBe(expectedDisposals,
                "each failed establishment attempt must dispose its enumerator to prevent resource leaks");
        }
    }
}
