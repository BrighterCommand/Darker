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
    /// FR11 case 2 — under native AOT, executing a <c>[QueryLogging]</c> query whose graph contains a
    /// reference cycle must NOT throw, because <c>QueryLoggingJsonOptions.Options</c> carries
    /// <c>ReferenceHandler.IgnoreCycles</c> by default (FR3). Returns 0 when execution completes without
    /// an exception (and a log entry was captured, proving the decorator actually serialised the graph);
    /// returns 1 if serialisation throws.
    /// </summary>
    internal static class Case2CycleBearingNoThrow
    {
        // Darker's pipeline resolves the handler's Execute method via reflection (PipelineBuilder's
        // documented AOT known-limitation). Root the handler's public methods so full-trim publish
        // keeps ExecuteAsync discoverable — consumer-side compensation, not a logging-path change.
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(AotCycleQueryHandler))]
        public static async Task<int> RunAsync()
        {
            // Arrange — install the capturing factory before the decorator's static Logger field is
            // first touched (install-before-touch: the closed generic caches its logger on first use).
            var entries = new List<CapturedLogEntry>();
            ApplicationLogging.LoggerFactory = new LoggerFactory(new[] { new CapturingLoggerProvider(entries) });

            // The source-generated resolver (AotTestJsonContext.Default) is process-global on the shared
            // QueryLoggingJsonOptions.Options and was already installed — and locked — by Case 1's first
            // serialize, so it is NOT (and cannot be) re-assigned here. What keeps the cyclic graph below
            // from throwing is the ReferenceHandler.IgnoreCycles default that same shared Options carries
            // (FR3). The resolver must contain AotCycleQuery's metadata for serialization to proceed.

            // Build a Parent -> Child -> Parent reference cycle.
            var root = new AotParent { Name = "Root" };
            var leaf = new AotChild { Name = "Leaf", Parent = root };
            root.Children.Add(leaf);
            var query = new AotCycleQuery(root);

            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AotCycleQuery, AotCycleQuery.Result, AotCycleQueryHandler>();

            var handlerFactory = new SimpleHandlerFactory(_ => new AotCycleQueryHandler());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(
                _ => new QueryLoggingDecoratorAsync<IQuery<AotCycleQuery.Result>, AotCycleQuery.Result>());

            var configuration = new HandlerConfiguration(
                new QueryHandlerRegistry(), handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                asyncRegistry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory);

            var processor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());

            // Act — the success assertion is simply "did not throw".
            try
            {
                await processor.ExecuteAsync(query);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Case2] FAIL: cycle-bearing query execution threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }

            // Assert — install-before-touch precondition: a captured start entry proves the decorator
            // actually serialised the cyclic graph (rather than the log being silently skipped).
            var start = entries.FirstOrDefault(e => e.MessageTemplate == "Executing async query {QueryName}: {Query}");
            if (start is null)
            {
                Console.Error.WriteLine(
                    "[Case2] FAIL: no start log entry captured — install-before-touch ordering broke.");
                return 1;
            }

            Console.WriteLine("[Case2] PASS: cycle-bearing query executed without throwing (IgnoreCycles default holds under AOT).");
            return 0;
        }
    }
}
