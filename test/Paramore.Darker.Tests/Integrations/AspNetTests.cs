using System;
using Microsoft.Extensions.DependencyInjection;
using Paramore.Darker.AspNetCore;
using Paramore.Darker.Testing.Ports;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests.Integrations
{
    public class AspNetTests
    {
        [Fact]
        public void HandlersGetWiredWithServiceCollection()
        {
            var services = new ServiceCollection();

            services.AddDarker().AddHandlersFromAssemblies(typeof(TestQueryHandler).Assembly);

            var queryProcessor = services.BuildServiceProvider().GetService<IQueryProcessor>();
            queryProcessor.ShouldNotBeNull();

            var id = Guid.NewGuid();
            var result = queryProcessor.Execute(new TestQueryA(id));
            result.ShouldBe(id);
        }
    }
}