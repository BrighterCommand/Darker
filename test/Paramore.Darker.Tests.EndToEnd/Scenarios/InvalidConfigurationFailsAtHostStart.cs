// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Tests.EndToEnd.Handlers;

namespace Paramore.Darker.Tests.EndToEnd.Scenarios
{
    /// <summary>
    /// FR10 - a handler whose decorator is not registered, in a chain that ends with ValidatePipelines, fails
    /// host start with a <see cref="PipelineValidationException"/> naming DecoratorResolvable and the handler.
    /// That failure is the expected outcome, so the scenario returns 0. Otherwise it prints what differed to
    /// <see cref="Console.Error"/> and returns 1.
    /// </summary>
    public static class InvalidConfigurationFailsAtHostStart
    {
        /// <summary>Runs the scenario.</summary>
        /// <returns>0 when host start fails as expected, otherwise 1.</returns>
        public static async Task<int> RunAsync()
        {
            // Arrange
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
            builder.Services.AddDarker()
                .AddAsyncHandlers(registry => registry.Register<UnregisteredDecoratorQuery, string, UnregisteredDecoratorQueryHandler>())
                .ValidatePipelines();

            using var host = builder.Build();

            try
            {
                // Act
                try
                {
                    await host.StartAsync();
                }
                catch (PipelineValidationException exception)
                {
                    // Assert
                    var failed = false;
                    if (!exception.Message.Contains("[DecoratorResolvable]", StringComparison.Ordinal))
                    {
                        Console.Error.WriteLine($"expected the message to contain [DecoratorResolvable], got {exception.Message}");
                        failed = true;
                    }

                    if (!exception.Message.Contains(nameof(UnregisteredDecoratorQueryHandler), StringComparison.Ordinal))
                    {
                        Console.Error.WriteLine($"expected the message to contain {nameof(UnregisteredDecoratorQueryHandler)}, got {exception.Message}");
                        failed = true;
                    }

                    return failed ? 1 : 0;
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"expected host start to throw PipelineValidationException, got {exception}");
                    return 1;
                }

                Console.Error.WriteLine("host started although the configuration is invalid");
                await host.StopAsync();
                return 1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"expected the scenario to complete, got {exception}");
                return 1;
            }
        }
    }
}
