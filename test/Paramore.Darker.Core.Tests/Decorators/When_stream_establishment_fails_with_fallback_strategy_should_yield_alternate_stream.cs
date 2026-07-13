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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Policies.Handlers;
using Polly;
using Polly.Registry;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Decorators
{
    public class StreamFallbackEstablishmentTests
    {
        [Fact]
        public async Task When_establishment_fails_with_fallback_strategy_should_yield_alternate_stream()
        {
            // Arrange
            const string pipelineName = "Fallback";
            var primaryDisposeCount = 0;
            var alternateDisposeCount = 0;

            // Primary handler always fails before yielding any item
            var primaryHandler = new TransientlyFailingStreamHandler(failuresBeforeSuccess: int.MaxValue);

            // Alternate source: the items the fallback stream should yield
            static string[] AlternateItems() => new[] { "fallback-a", "fallback-b" };
            var countingAlternateSource = new DisposalCountingEnumerable<string>(
                AsyncSource(AlternateItems()), () => alternateDisposeCount++);

            // Resilience pipeline: when Establish faults, substitute the alternate (enumerator, moved)
            // The untyped pipeline boxes TResult to object internally; the factory returns a boxed
            // (IAsyncEnumerator<string>, bool) that Polly unboxes for the decorator.
            var registry = new ResiliencePipelineRegistry<string>();
            registry.TryAddBuilder(pipelineName, (builder, _) =>
                builder.AddStrategy(_ => new EstablishmentFallbackStrategy(async ct =>
                {
                    var e = countingAlternateSource.GetAsyncEnumerator(ct);
                    var moved = await e.MoveNextAsync();
                    return (object)(e, moved);
                })));

            var context = new QueryContext { ResiliencePipeline = registry };
            var decorator = new UseResiliencePipelineStreamHandler<MultiItemStreamQuery, string>
            {
                Context = context
            };
            decorator.InitializeFromAttributeParams(new object[] { pipelineName });

            var query = new MultiItemStreamQuery();

            // Act — primary fails; fallback substitutes the alternate stream at establishment
            var results = new List<string>();
            await foreach (var item in decorator.Execute(
                query,
                (q, ct) => new DisposalCountingEnumerable<string>(primaryHandler.ExecuteAsync(q, ct), () => primaryDisposeCount++),
                default))
            {
                results.Add(item);
            }

            // Assert — alternate stream items yielded (fallback path taken)
            results.ShouldBe(AlternateItems());

            // Primary enumerator disposed by Establish's catch before fallback fired: no primary leak
            primaryDisposeCount.ShouldBe(1,
                "primary enumerator must be disposed inside Establish's catch before the fallback fires");

            // Alternate enumerator is the only one owned by the outer await using
            alternateDisposeCount.ShouldBe(1,
                "the fallback-substituted enumerator is disposed exactly once by the outer await using");
        }

        private static async IAsyncEnumerable<string> AsyncSource(
            string[] items,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var item in items)
            {
                ct.ThrowIfCancellationRequested();
                yield return item;
            }
            await Task.CompletedTask;
        }
    }
}
