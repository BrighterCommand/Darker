using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.PipelineValidation;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    [Collection("DarkerHostBootstrap")]
    public class HostStartInertScopeAffinityTests
    {
        [Fact]
        public async Task When_the_host_starts_with_an_inert_scope_affinity_should_fail_start()
        {
            // Arrange — a Transient handler lifetime with a JoinAmbient default is inert; throwOnError defaults to true
            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddDarker(o =>
                {
                    o.HandlerLifetime = ServiceLifetime.Transient;
                    o.DefaultScopeAffinity = ScopeAffinity.JoinAmbient;
                })
                .ValidatePipelines();
            // The host is deliberately not disposed: that would dispose its real ILoggerFactory, which Darker keeps in static ApplicationLogging.
            var host = builder.Build();

            // Act — start the host
            var exception = await Should.ThrowAsync<PipelineValidationException>(() => host.StartAsync(TestContext.Current.CancellationToken));

            // Assert — the message lists the InertScopeAffinity finding
            exception.Message.ShouldContain("[InertScopeAffinity]");
        }
    }
}
