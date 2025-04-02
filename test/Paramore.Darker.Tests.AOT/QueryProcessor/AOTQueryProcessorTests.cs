using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Testing.Ports;
using Paramore.Darker.Tests.AOT.Helpers.Base;
using Shouldly;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Paramore.Darker.Tests.AOT.QueryProcessor
{
    /// <summary>
    /// Contains unit tests for the <see cref="IQueryProcessor"/> implementation in the AOT context.
    /// This class ensures that the <see cref="IQueryProcessor"/> and its dependencies are correctly configured
    /// and function as expected within the test environment.
    /// </summary>
    /// <remarks>
    /// Inherits from <see cref="TestClassBase"/> to utilize shared setup and utility functionality for test execution.
    /// </remarks>
    public class AOTQueryProcessorTests : TestClassBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AOTQueryProcessorTests"/> class.
        /// This constructor sets up the test environment by initializing the base class with the provided test output helper.
        /// </summary>
        /// <param name="testOutputHelper">
        /// An instance of <see cref="ITestOutputHelper"/> used to capture and manage test output during execution.
        /// </param>
        public AOTQueryProcessorTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
        {
        }

        /// <summary>
        /// Gets the <see cref="IQueryProcessor"/> instance from the service provider.
        /// </summary>
        /// <remarks>
        /// The <see cref="IQueryProcessor"/> is used to execute queries in the application.
        /// It is resolved from the <see cref="ServiceProvider"/> and provides methods for both synchronous
        /// and asynchronous query execution.
        /// </remarks>
        protected IQueryProcessor QueryProcessor => ServiceProvider.GetRequiredService<IQueryProcessor>();

        [Fact]
        public void AOTQueryProcessor_ServiceProvider()
        {
            ServiceProvider.ShouldNotBeNull();
        }

        [Fact]
        public void AOTQueryProcessor_Logger()
        {
            ServiceProvider.ShouldNotBeNull();
            ILogger logger = ServiceProvider.GetRequiredService<ILogger<TestClassBase>>();
            logger.ShouldNotBeNull();
            logger.LogInformation("Test Information");
        }

        [Fact]
        public void AOTQueryProcessor_Logger_BeginScope()
        {
            ServiceProvider.ShouldNotBeNull();
            ILogger logger = ServiceProvider.GetRequiredService<ILogger<TestClassBase>>();
            logger.ShouldNotBeNull();

            using (logger.BeginScope("TestScope"))
            {
                logger.LogInformation("Test Information");
            }
        }

        [Fact]
        public void AOTQueryProcessor_ServiceProvider_QueryProcessor()
        {
            ServiceProvider.ShouldNotBeNull();
            QueryProcessor.ShouldNotBeNull();
            ServiceProvider.GetRequiredService<IQueryProcessor>().ShouldNotBeNull();
            ServiceProvider.GetRequiredService<IQueryProcessor>().ShouldBe(QueryProcessor);
        }

        [Fact]
        public void AOTQueryProcessor_QueryProcessor_Execute()
        {
            var id = Guid.NewGuid();
            QueryProcessor.ShouldNotBeNull();

            var query = new TestQueryA(id);
            var result = QueryProcessor.Execute(query);
            result.ShouldNotBe(Guid.Empty);

            result.ShouldBe(id);
        }

        [Fact]
        public async Task AOTQueryProcessor_QueryProcessor_ExecuteAsync()
        {
            var id = Guid.NewGuid();
            QueryProcessor.ShouldNotBeNull();

            var query = new TestQueryA(id);
            var result = await QueryProcessor.ExecuteAsync(query);
            result.ShouldNotBe(Guid.Empty);

            result.ShouldBe(id);
        }
    }
}