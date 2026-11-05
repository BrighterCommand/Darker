using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Paramore.Darker.PipelineValidation;
using Paramore.Darker.Specifications;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    [Collection("DarkerHostBootstrap")]
    public class StartAsyncOnlyValidationTests
    {
        [Fact]
        public async Task When_only_start_async_is_called_should_validate()
        {
            // Arrange — a counting validator beside ValidatePipelines, and a host that never calls StartingAsync
            var counting = new CountingValidator();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDarker().ValidatePipelines();
            services.AddSingleton<IAmAPipelineValidator>(counting);
            // Deliberately not disposed: that would dispose the real ILoggerFactory, which Darker keeps in static ApplicationLogging.
            var provider = services.BuildServiceProvider();

            // Act — call only StartAsync, once, on each hosted service
            foreach (var hosted in provider.GetServices<IHostedService>())
                await hosted.StartAsync(TestContext.Current.CancellationToken);

            // Assert — validation ran exactly once
            counting.Count.ShouldBe(1);
        }

        [Fact]
        public async Task When_only_start_async_is_called_and_validation_finds_an_error_should_throw()
        {
            // Arrange — throwOnError true and a validator that returns an error
            var error = new ValidationError(ValidationSeverity.Error, "StartAsyncSource", "start async error message")
            {
                RuleId = new ValidationRuleId("StartAsyncRule")
            };
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDarker().ValidatePipelines();
            services.AddSingleton<IAmAPipelineValidator>(new FixedResultValidator(new PipelineValidationResult([error], [])));
            // Deliberately not disposed: that would dispose the real ILoggerFactory, which Darker keeps in static ApplicationLogging.
            var provider = services.BuildServiceProvider();

            // Act — call only StartAsync
            var exception = await Should.ThrowAsync<PipelineValidationException>(async () =>
            {
                foreach (var hosted in provider.GetServices<IHostedService>())
                    await hosted.StartAsync(TestContext.Current.CancellationToken);
            });

            // Assert — the message lists the finding
            exception.Message.ShouldContain("StartAsyncRule");
            exception.Message.ShouldContain("StartAsyncSource");
        }
    }
}
