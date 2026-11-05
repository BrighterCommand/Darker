using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.DependencyInjection;
using Paramore.Darker.Extensions.Tests.TestDoubles;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Extensions.Tests.PipelineValidation
{
    [Collection("DarkerHostBootstrap")]
    public class ValidateAllScopeCreationTests
    {
        [Fact]
        public void When_ValidateAll_is_called_should_create_no_scope()
        {
            // Arrange — both validators registered, and the built provider wrapped so scope creation is counted
            var services = new ServiceCollection();
            services.AddSingleton<IAmAScopeProvider, TestScopeProvider>();
            services.AddDarker(o => o.HandlerLifetime = ServiceLifetime.Scoped)
                .AddHandlers(r => r.Register<CaptiveQuery, CaptiveResult, CaptiveHandler>())
                .ValidatePipelines();
            services.AddScoped<ScopedDependency>();
            // Not disposed: AddDarker stores the real ILoggerFactory in a static, and disposing it would break later tests.
            var provider = new ScopeRecordingServiceProvider(services.BuildServiceProvider());

            // Act
            provider.ValidateAll();

            // Assert — validation resolved its validators from the root provider and created no scope
            provider.ScopeFactory.ScopesCreated.ShouldBe(0);
        }
    }
}
