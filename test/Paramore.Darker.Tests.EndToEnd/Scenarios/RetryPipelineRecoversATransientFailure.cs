// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Tests.EndToEnd.Handlers;
using Polly;
using Polly.Registry;
using Polly.Retry;

namespace Paramore.Darker.Tests.EndToEnd.Scenarios
{
    /// <summary>
    /// FR8, NFR4 - an async handler that fails on its first call and is decorated with a retry
    /// resilience pipeline returns its result after exactly two calls. There is no delay and no wait:
    /// the retry delay is zero and failure is decided by call count only. Returns 0 on success; prints
    /// what differed to <see cref="Console.Error"/> and returns 1 otherwise.
    /// </summary>
    public static class RetryPipelineRecoversATransientFailure
    {
        /// <summary>Runs the scenario.</summary>
        /// <returns>0 when every check passes, otherwise 1.</returns>
        public static async Task<int> RunAsync()
        {
            // Arrange
            var pipelines = new ResiliencePipelineRegistry<string>();
            pipelines.TryAddBuilder(RetriedQueryHandler.RetryPipelineName, (pipeline, _) =>
                pipeline.AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<TransientFailureException>(),
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.Zero
                }));

            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            builder.Services.AddSingleton<CallCounter>();
            builder.Services.AddDarker()
                .AddAsyncHandlers(registry => registry.Register<RetriedQuery, string, RetriedQueryHandler>())
                .AddResiliencePipelines(pipelines)
                .ValidatePipelines();

            using var host = builder.Build();

            try
            {
                // Act
                await host.StartAsync();
                var processor = host.Services.GetRequiredService<IQueryProcessor>();
                string result;
                try
                {
                    result = await processor.ExecuteAsync(new RetriedQuery());
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"expected the retry pipeline to recover the transient failure, got {exception}");
                    return 1;
                }

                var calls = host.Services.GetRequiredService<CallCounter>().Count;
                await host.StopAsync();

                // Assert
                var failed = false;
                if (result != "recovered")
                {
                    Console.Error.WriteLine($"expected retried query to return \"recovered\", got \"{result}\"");
                    failed = true;
                }

                if (calls != 2)
                {
                    Console.Error.WriteLine($"expected the handler to be called 2 times, got {calls}");
                    failed = true;
                }

                return failed ? 1 : 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"expected the host to start and run the query, got {exception}");
                return 1;
            }
        }
    }
}
