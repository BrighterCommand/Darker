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

namespace Paramore.Darker.Tests.EndToEnd.Scenarios
{
    /// <summary>
    /// FR9 - a Scoped handler that takes a Scoped disposable dependency receives an undisposed
    /// instance while the query runs, and the dependency is disposed once the query ends. Returns 0 on
    /// success; prints what differed to <see cref="Console.Error"/> and returns 1 otherwise.
    /// </summary>
    public static class ScopedDependencyIsProvidedAndDisposed
    {
        /// <summary>Runs the scenario.</summary>
        /// <returns>0 when every check passes, otherwise 1.</returns>
        public static async Task<int> RunAsync()
        {
            // Arrange
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            builder.Services.AddScoped<ScopedDependency>();
            builder.Services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
                .AddAsyncHandlers(registry => registry.Register<ScopedDependencyQuery, ScopedDependencyObservation, ScopedDependencyQueryHandler>())
                .ValidatePipelines();

            using var host = builder.Build();

            try
            {
                // Act
                await host.StartAsync();
                var processor = host.Services.GetRequiredService<IQueryProcessor>();
                ScopedDependencyObservation observation;
                try
                {
                    observation = await processor.ExecuteAsync(new ScopedDependencyQuery());
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"expected the query to run, got {exception}");
                    return 1;
                }

                await host.StopAsync();

                // Assert
                var failed = false;
                if (observation.Dependency is null)
                {
                    Console.Error.WriteLine("expected the handler to receive a dependency instance, got null");
                    return 1;
                }

                if (observation.WasDisposedDuringQuery)
                {
                    Console.Error.WriteLine("expected the dependency to be undisposed while the handler ran, but it was disposed");
                    failed = true;
                }

                if (!observation.Dependency.IsDisposed)
                {
                    Console.Error.WriteLine("expected the dependency to be disposed after the query ended, but it was not disposed");
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
