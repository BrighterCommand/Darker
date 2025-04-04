using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Testing.Ports;
using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Paramore.Darker.AspNetCore;
using Paramore.Darker.Policies;
using Paramore.Darker.QueryLogging;
using Paramore.Test.Helpers.Loggers;
using Paramore.Test.Helpers.TestOutput;
using Xunit.Abstractions;

namespace Paramore.Test.Helpers.Base
{
    /// <summary>
    /// Serves as a base class for test classes in the AOT test suite.
    /// This abstract class provides shared setup and utility functionality for derived test classes.
    /// Implements the <see cref="Xunit.IClassFixture{TFixture}"/> interface to support shared test context.
    /// </summary>
    /// <typeparam name="T">
    /// The type parameter representing the specific test class deriving from this base class.
    /// </typeparam>
    public abstract class AOTTestClassBase<T> : TestClassBase<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AOTTestClassBase{T}"/> class.
        /// This constructor is used to set up the shared test context and output helper for derived test classes.
        /// </summary>
        /// <param name="testOutputHelper">
        /// An instance of <see cref="ITestOutputHelper"/> used to capture test output during execution.
        /// </param>
        protected AOTTestClassBase(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
        {
        }

        /// <summary>
        /// Builds and configures an <see cref="IServiceProvider"/> instance using the provided <see cref="IServiceCollection"/> 
        /// and a test output helper.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to configure.</param>
        /// <param name="testOutputHelper">The <see cref="ICoreTestOutputHelper"/> used for logging test output.</param>
        /// <returns>An <see cref="IServiceProvider"/> instance with the configured services.</returns>
        /// <remarks>
        /// This method sets up the necessary services and configurations for testing with the Darker library. 
        /// It includes the following configurations:
        /// - Registers the test output helper for logging.
        /// - Configures logging to use the test output logger.
        /// - Adds Darker query handlers from the specified assemblies.
        /// - Enables JSON-based query logging.
        /// - Configures default retry and circuit breaker policies.
        /// </remarks>
        protected override IServiceProvider BuildServiceProvider(IServiceCollection services, ICoreTestOutputHelper testOutputHelper)
        {
            services.AddSingleton(testOutputHelper);
            services.AddLogging(builder =>
            {
                builder.Services.AddSingleton<ITestOutputLoggingProvider, TestOutputLoggingProvider>();
                builder.Services.AddSingleton<ILoggerProvider, TestOutputLoggingProvider>(provider => (provider.GetRequiredService<ITestOutputLoggingProvider>() as TestOutputLoggingProvider)!);

                // Replace existing logging with test output logger
                builder.Services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger), typeof(TestOutputLogger)));
                builder.Services.TryAdd(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(TestOutputLogger<>)));
            });

            services.AddDarker()
                .AddHandlersFromAssemblies(typeof(TestQueryA).Assembly)
                .AddJsonQueryLogging()
                .AddDefaultPolicies();

            return services.BuildServiceProvider();
        }
    }
}
