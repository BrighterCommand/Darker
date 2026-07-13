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
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests
{
    public class When_stream_handler_has_multiple_decorators_should_order_by_step_descending
    {
        private static QueryProcessor BuildProcessor(List<int> enteredSteps)
        {
            var streamRegistry = new StreamQueryHandlerRegistry();
            streamRegistry.Register<StepOrderStreamQuery, string, StepOrderStreamHandler>();

            var syncRegistry = new QueryHandlerRegistry();
            var handlerFactory = new SimpleHandlerFactory(_ => new StepOrderStreamHandler());
            // Decorator factory creates StreamStepEventDecorator with the shared recording list;
            // the step number is set later via InitializeFromAttributeParams.
            var decoratorFactory = new SimpleHandlerDecoratorFactory(_ =>
                new StreamStepEventDecorator<IStreamQuery<string>, string>(enteredSteps));
            var decoratorRegistry = new InMemoryDecoratorRegistry();

            var config = new HandlerConfiguration(
                syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
                new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                streamRegistry);

            return new QueryProcessor(config, new InMemoryQueryContextFactory());
        }

        [Fact]
        public async Task When_stream_handler_has_multiple_decorators_should_execute_them_ordered_by_step_descending()
        {
            // Arrange — StepOrderStreamHandler declares [StreamStepEvent(2)] and [StreamStepEvent(1)]
            var enteredSteps = new List<int>();
            var processor = BuildProcessor(enteredSteps);

            // Act
            var results = new List<string>();
            await foreach (var item in processor.ExecuteStream(new StepOrderStreamQuery()))
                results.Add(item);

            // Assert — step 2 entered first (outermost/highest step wraps first), then step 1
            enteredSteps.Count.ShouldBe(2, "both decorators must execute");
            enteredSteps[0].ShouldBe(2, "higher Step executes first (outermost decorator)");
            enteredSteps[1].ShouldBe(1, "lower Step executes second (inner decorator)");
            results.ShouldBe(StepOrderStreamHandler.Items);
        }
    }
}
