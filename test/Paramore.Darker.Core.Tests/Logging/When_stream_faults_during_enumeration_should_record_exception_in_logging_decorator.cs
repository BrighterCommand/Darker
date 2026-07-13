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
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Core.Tests.TestDoubles;
using Paramore.Darker.Logging;
using Paramore.Darker.Logging.Handlers;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Core.Tests.Logging
{
    [Collection("QueryLoggingJsonOptions")]
    public class When_stream_faults_during_enumeration_should_record_exception_in_logging_decorator
    {
        private readonly LoggerCaptureFixture _logs;

        public When_stream_faults_during_enumeration_should_record_exception_in_logging_decorator(LoggerCaptureFixture logs)
        {
            _logs = logs;
        }

        [Fact]
        public async Task When_stream_handler_throws_mid_enumeration_should_log_error_and_propagate_exception()
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
                streamRegistry.Register<FaultingStreamQuery, string, LoggedFaultingStreamHandler>();

                var handlerFactory = new SimpleHandlerFactory(_ => new LoggedFaultingStreamHandler());
                var decoratorFactory = new SimpleHandlerDecoratorFactory(
                    _ => new StreamQueryLoggingDecorator<IStreamQuery<string>, string>());
                var decoratorRegistry = new InMemoryDecoratorRegistry();

                var config = new HandlerConfiguration(
                    new QueryHandlerRegistry(), handlerFactory, decoratorRegistry, decoratorFactory,
                    new QueryHandlerRegistryAsync(), handlerFactory, decoratorRegistry, decoratorFactory,
                    streamRegistry);

                var processor = new QueryProcessor(config, new InMemoryQueryContextFactory());

                // Act — enumerate; expect fault after the items yielded before the throw
                var items = new List<string>();
                Exception caughtException = null;
                try
                {
                    await foreach (var item in processor.ExecuteStream(new FaultingStreamQuery()))
                        items.Add(item);
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                }

                // Assert — items yielded before the fault were received by the caller
                items.Count.ShouldBe(FaultingStreamHandler.ItemsBeforeFault,
                    "items produced before the fault must reach the caller");

                // Assert — original exception propagated (not swallowed by the decorator)
                caughtException.ShouldNotBeNull("the logging decorator must not swallow exceptions");
                caughtException.ShouldBeOfType<InvalidOperationException>();
                caughtException.Message.ShouldBe(FaultingStreamHandler.ExceptionMessage);

                // Assert — error-level log was recorded with the exception
                var errorLog = _logs.CapturedLogs.FirstOrDefault(e => e.LogLevel == LogLevel.Error);
                errorLog.ShouldNotBeNull("logging decorator must emit an Error log when the stream faults");
                errorLog.Exception.ShouldNotBeNull();
                errorLog.Exception.Message.ShouldBe(FaultingStreamHandler.ExceptionMessage,
                    "the logged exception must be the original fault from the handler");
                Argument(errorLog, "QueryName").ShouldBe(nameof(FaultingStreamQuery));
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
