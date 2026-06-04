// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Paramore.Darker;
using Paramore.Darker.Logging;
using Paramore.Darker.Logging.Handlers;
using Paramore.Darker.Tests.AOT.Logging;

namespace Paramore.Darker.Tests.AOT.Scenarios
{
    /// <summary>
    /// FR11 case 1 — under native AOT, executing a property-bearing <c>[QueryLogging]</c> query must
    /// log the <c>{Query}</c> argument as the exact source-generated JSON. Returns 0 on a match and
    /// prints an expected/actual diff and returns 1 on mismatch (or if serialization throws because
    /// no source-generated resolver is installed under AOT).
    /// </summary>
    internal static class Case1PropertyBearingJson
    {
        // STJ defaults Guid format to "D" (lowercase, hyphenated); the positional record emits Id then Name.
        private const string ExpectedQueryJson = "{\"Id\":\"11111111-1111-1111-1111-111111111111\",\"Name\":\"Ada\"}";

        // Darker's pipeline resolves the handler's Execute method via reflection (PipelineBuilder's
        // documented AOT known-limitation). Root the handler's public methods so full-trim publish
        // keeps ExecuteAsync discoverable — this is the consumer-side compensation the release notes
        // call out, not a change to the logging path under test.
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(AotLoggedQueryHandler))]
        public static async Task<int> RunAsync()
        {
            // Arrange — install the capturing factory before the decorator's static Logger field is
            // first touched (install-before-touch: the closed generic caches its logger on first use).
            var entries = new List<CapturedLogEntry>();
            ApplicationLogging.LoggerFactory = new LoggerFactory(new[] { new CapturingLoggerProvider(entries) });

            // Install the source-generated resolver so the decorator's serialization is AOT-safe (NFR2).
            QueryLoggingJsonOptions.Options.TypeInfoResolver = AotTestJsonContext.Default;

            var query = new AotLoggedQuery(new Guid("11111111-1111-1111-1111-111111111111"), "Ada");

            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AotLoggedQuery, AotLoggedQuery.Result, AotLoggedQueryHandler>();

            var handlerFactory = new SimpleHandlerFactory(_ => new AotLoggedQueryHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(
                _ => new QueryLoggingDecoratorAsync<IQuery<AotLoggedQuery.Result>, AotLoggedQuery.Result>());

            var configuration = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                asyncRegistry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory);

            var processor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());

            // Act
            try
            {
                await processor.ExecuteAsync(query);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Case1] FAIL: query execution threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }

            // Assert — install-before-touch precondition, then exact source-generated JSON.
            var start = entries.FirstOrDefault(e => e.MessageTemplate == "Executing async query {QueryName}: {Query}");
            if (start is null)
            {
                Console.Error.WriteLine(
                    "[Case1] FAIL: no start log entry captured — install-before-touch ordering broke.");
                return 1;
            }

            var actual = start.Arguments.First(a => a.Key == "Query").Value as string;
            if (actual != ExpectedQueryJson)
            {
                Console.Error.WriteLine("[Case1] FAIL: {Query} did not match the expected source-generated JSON.");
                Console.Error.WriteLine($"  expected: {ExpectedQueryJson}");
                Console.Error.WriteLine($"  actual:   {actual ?? "<null>"}");
                return 1;
            }

            Console.WriteLine("[Case1] PASS: property-bearing query logged the expected source-generated JSON.");
            return 0;
        }
    }
}
