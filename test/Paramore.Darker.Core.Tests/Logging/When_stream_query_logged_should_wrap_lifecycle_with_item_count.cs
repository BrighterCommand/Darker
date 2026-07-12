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
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Logging;
using Paramore.Darker.Logging.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Logging
{
    [Collection("QueryLoggingJsonOptions")]
    public class When_stream_query_logged_should_wrap_lifecycle_with_item_count
    {
        private readonly LoggerCaptureFixture _logs;

        public When_stream_query_logged_should_wrap_lifecycle_with_item_count(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public async Task When_stream_query_executes_with_logging_decorator_should_log_start_and_completion_with_item_count()
        {
            // Arrange — throwaway options so the serialize-lock never touches the shared default (C5)
            var original = QueryLoggingJsonOptions.Options;
            _logs.Clear();
            try
            {
                QueryLoggingJsonOptions.Options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    WriteIndented = false
                };

                var streamRegistry = new StreamQueryHandlerRegistry();
                streamRegistry.Register<MultiItemStreamQuery, string, LoggedMultiItemStreamHandler>();

                var handlerFactory = new SimpleHandlerFactory(_ => new LoggedMultiItemStreamHandler());
                var decoratorFactory = new SimpleHandlerDecoratorFactory(
                    _ => new StreamQueryLoggingDecorator<IStreamQuery<string>, string>());
                var decoratorRegistry = new InMemoryDecoratorRegistry();

                var config = new HandlerConfiguration(
                    new QueryHandlerRegistry(), handlerFactory, decoratorRegistry, decoratorFactory,
                    new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                    streamRegistry);

                var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

                // Act — enumerate the full stream
                var items = new List<string>();
                await foreach (var item in processor.ExecuteStream(new MultiItemStreamQuery()))
                    items.Add(item);

                // Assert — all items yielded unchanged (decorator is pass-through)
                items.ShouldBe(MultiItemStreamHandler.Items, ignoreOrder: false);

                // Assert — start log emitted before any items
                var start = _logs.CapturedLogs.FirstOrDefault(
                    e => e.MessageTemplate == "Executing stream query {QueryName}: {Query}");
                start.ShouldNotBeNull("logging decorator must emit a start log with the query name and body");
                Argument(start, "QueryName").ShouldBe(nameof(MultiItemStreamQuery));

                // Assert — completion log emitted with item count and elapsed
                var completion = _logs.CapturedLogs.FirstOrDefault(
                    e => e.MessageTemplate == "Stream execution of query {QueryName} completed; {ItemCount} items in {Elapsed}ms");
                completion.ShouldNotBeNull("logging decorator must emit a completion log with item count and elapsed duration");
                Argument(completion, "QueryName").ShouldBe(nameof(MultiItemStreamQuery));
                ((int)Argument(completion, "ItemCount")).ShouldBe(MultiItemStreamHandler.Items.Length,
                    "item count in the completion log must equal the number of items yielded");
            }
            finally
            {
                QueryLoggingJsonOptions.Options = original;
            }
        }

        [Fact]
        public async Task When_stream_query_with_logging_decorator_items_are_not_buffered()
        {
            // Arrange — throwaway options so the serialize-lock never touches the shared default (C5)
            var original = QueryLoggingJsonOptions.Options;
            _logs.Clear();
            try
            {
                QueryLoggingJsonOptions.Options = new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.IgnoreCycles,
                    WriteIndented = false
                };

                var streamRegistry = new StreamQueryHandlerRegistry();
                streamRegistry.Register<MultiItemStreamQuery, string, LoggedMultiItemStreamHandler>();

                var handlerFactory = new SimpleHandlerFactory(_ => new LoggedMultiItemStreamHandler());
                var decoratorFactory = new SimpleHandlerDecoratorFactory(
                    _ => new StreamQueryLoggingDecorator<IStreamQuery<string>, string>());
                var decoratorRegistry = new InMemoryDecoratorRegistry();

                var config = new HandlerConfiguration(
                    new QueryHandlerRegistry(), handlerFactory, decoratorRegistry, decoratorFactory,
                    new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                    streamRegistry);

                var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

                // Act — break after the first item; the stream is cold (lazy) so completion log includes only 1
                var items = new List<string>();
                await foreach (var item in processor.ExecuteStream(new MultiItemStreamQuery()))
                {
                    items.Add(item);
                    break;
                }

                // Assert — only one item received (early break worked)
                items.Count.ShouldBe(1);

                // Assert — completion log reflects the actual items observed, not the total buffered count
                var completion = _logs.CapturedLogs.FirstOrDefault(
                    e => e.MessageTemplate == "Stream execution of query {QueryName} completed; {ItemCount} items in {Elapsed}ms");
                completion.ShouldNotBeNull("completion log must be emitted even on early break (finally block)");
                ((int)Argument(completion, "ItemCount")).ShouldBe(1,
                    "item count must be 1 because the consumer broke after the first item — the decorator does not buffer");
            }
            finally
            {
                QueryLoggingJsonOptions.Options = original;
            }
        }

        private static object Argument(CapturedLogEntry entry, string key)
            => entry.StructuredArguments.Single(kvp => kvp.Key == key).Value;
    }
}
