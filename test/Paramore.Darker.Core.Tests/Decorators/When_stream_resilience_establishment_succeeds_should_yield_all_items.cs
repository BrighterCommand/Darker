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
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Decorators
{
    public class StreamResilienceHappyPathTests
    {
        [Fact]
        public async Task When_stream_uses_resilience_pipeline_and_establishment_succeeds_should_yield_all_items()
        {
            // Arrange — a no-op pipeline (long timeout, never fires) that lets everything succeed
            const string pipelineName = "NoOp";
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(pipelineName, (builder, _) =>
                builder.AddTimeout(TimeSpan.FromMinutes(1)));

            var context = new QueryContext { ResiliencePipeline = registry };
            var decorator = new UseResiliencePipelineStreamHandler<MultiItemStreamQuery, string>
            {
                Context = context
            };
            decorator.InitializeFromAttributeParams(new object[] { pipelineName });

            var query = new MultiItemStreamQuery();
            var handler = new MultiItemStreamHandler { Context = context };

            // Act — establishment succeeds: pipeline wraps first MoveNextAsync then yields all items
            var results = new List<string>();
            await foreach (var item in decorator.Execute(
                query,
                (q, ct) => handler.ExecuteAsync(q, ct),
                default))
            {
                results.Add(item);
            }

            // Assert — all handler items pass through the decorator unchanged
            results.ShouldBe(MultiItemStreamHandler.Items);
        }
    }
}
