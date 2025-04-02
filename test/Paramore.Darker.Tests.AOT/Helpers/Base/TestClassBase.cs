using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Paramore.Darker.AspNetCore;
using Paramore.Darker.Policies;
using Paramore.Darker.QueryLogging;
using Paramore.Darker.Testing.Ports;
using Paramore.Darker.Tests.AOT.Helpers.Extensions;
using Paramore.Darker.Tests.AOT.Helpers.Loggers;
using Paramore.Darker.Tests.AOT.Helpers.TestOutput;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Xunit.Abstractions;

namespace Paramore.Darker.Tests.AOT.Helpers.Base
{
    /// <summary>
    /// Serves as a base class for test classes in the AOT test suite.
    /// This abstract class provides shared setup and utility functionality for derived test classes.
    /// Implements the <see cref="Xunit.IClassFixture{TFixture}"/> interface to support shared test context.
    /// </summary>
    public abstract class TestClassBase : ITestClassBase
    {
        private bool _disposedValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestClassBase"/> class.
        /// This constructor is used to set up the shared test context and output helper for derived test classes.
        /// </summary>
        /// <param name="testOutputHelper">
        /// An instance of <see cref="ITestOutputHelper"/> used to capture test output during execution.
        /// </param>
        protected TestClassBase(ITestOutputHelper testOutputHelper)
        {
            TestOutputHelper = new CoreTestOutputHelper(this, testOutputHelper);
            ServiceProvider = BuildServiceProvider(new ServiceCollection(), TestOutputHelper);
            XunitTest = (ITest)GetTestField(testOutputHelper)?.GetValue(testOutputHelper)!;
        }

        /// <inheritdoc />
        public IServiceProvider ServiceProvider { get; }

        /// <inheritdoc />
        public ICoreTestOutputHelper TestOutputHelper { get; }

        /// <inheritdoc />
        public ITest XunitTest { get; }

        /// <inheritdoc />
        public string TestQualifiedName => XunitTest.DisplayName;

        /// <inheritdoc />
        public string TestDisplayName => XunitTest.DisplayName.RemoveNamespace();

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
        public static IServiceProvider BuildServiceProvider(IServiceCollection services, ICoreTestOutputHelper testOutputHelper)
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

        /// <summary>
        /// Releases all resources used by the <see cref="TestClassBase"/> instance.
        /// </summary>
        /// <remarks>
        /// This method is responsible for performing cleanup operations for the current instance of the class.
        /// It ensures that any unmanaged resources are released and any disposable objects are properly disposed.
        /// </remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    TestOutputHelper.Dispose();
                }

                _disposedValue = true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Retrieves the private field named 'test' from the specified <see cref="ITestOutputHelper"/> instance.
        /// </summary>
        /// <param name="testOutputHelper">
        /// An instance of <see cref="ITestOutputHelper"/> from which the private field is to be retrieved.
        /// </param>
        /// <returns>
        /// A <see cref="FieldInfo"/> object representing the 'test' field if it exists; otherwise, <c>null</c>.
        /// </returns>
        /// <remarks>
        /// This method uses reflection to access a private field named 'test' within the provided 
        /// <see cref="ITestOutputHelper"/> instance. The field is expected to exist in the implementation.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="testOutputHelper"/> parameter is <c>null</c>.
        /// </exception>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075", Justification = "The field 'test' is known to exist in the ITestOutputHelper implementation.")]
        private static FieldInfo? GetTestField(ITestOutputHelper testOutputHelper)
        {
            return testOutputHelper.GetType().GetField("test", BindingFlags.Instance | BindingFlags.NonPublic);
        }
    }
}