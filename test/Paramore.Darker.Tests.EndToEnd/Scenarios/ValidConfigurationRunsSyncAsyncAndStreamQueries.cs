// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Tests.EndToEnd.Handlers;

namespace Paramore.Darker.Tests.EndToEnd.Scenarios
{
    /// <summary>
    /// FR6, FR7, FR11 - a Generic Host with no web server, configured through <c>AddDarker</c> and
    /// ending with <c>ValidatePipelines()</c>, starts and returns the expected results for a sync,
    /// an async and a stream query. Returns 0 on success; prints what differed to
    /// <see cref="Console.Error"/> and returns 1 otherwise.
    /// </summary>
    public static class ValidConfigurationRunsSyncAsyncAndStreamQueries
    {
        /// <summary>Runs the scenario.</summary>
        /// <returns>0 when every check passes, otherwise 1.</returns>
        public static async Task<int> RunAsync()
        {
            // Arrange
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            builder.Services.AddDarker()
                .AddHandlers(registry => registry.Register<SyncQuery, string, SyncQueryHandler>())
                .AddAsyncHandlers(registry => registry.Register<AsyncQuery, string, AsyncQueryHandler>())
                .AddStreamHandlers(registry => registry.Register<StreamQuery, int, StreamQueryHandler>())
                .ValidatePipelines();

            using var host = builder.Build();

            try
            {
                // Act
                await host.StartAsync();
                var processor = host.Services.GetRequiredService<IQueryProcessor>();

                var syncResult = processor.Execute(new SyncQuery());
                var asyncResult = await processor.ExecuteAsync(new AsyncQuery());
                var streamResult = new List<int>();
                await foreach (var item in processor.ExecuteStream(new StreamQuery()))
                {
                    streamResult.Add(item);
                }

                await host.StopAsync();

                // Assert
                var failed = false;
                if (syncResult != "sync")
                {
                    Console.Error.WriteLine($"expected sync query to return \"sync\", got \"{syncResult}\"");
                    failed = true;
                }

                if (asyncResult != "async")
                {
                    Console.Error.WriteLine($"expected async query to return \"async\", got \"{asyncResult}\"");
                    failed = true;
                }

                var streamText = string.Join(",", streamResult);
                if (streamText != "1,2,3")
                {
                    Console.Error.WriteLine($"expected stream query to yield 1,2,3, got {streamText}");
                    failed = true;
                }

                return failed ? 1 : 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"expected the host to start and run the queries, got {exception}");
                return 1;
            }
        }
    }
}
