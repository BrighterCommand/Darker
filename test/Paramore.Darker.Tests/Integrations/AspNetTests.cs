using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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

            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                //builder.AddConsole();
                //builder.AddDebug();
            });

            services.AddSingleton<ILoggerFactory>(loggerFactory);

            services.AddDarker().AddHandlersFromAssemblies(typeof(TestQueryHandler).Assembly);

            var queryProcessor = services.BuildServiceProvider().GetService<IQueryProcessor>();
            queryProcessor.ShouldNotBeNull();

            var id = Guid.NewGuid();
            var result = queryProcessor.Execute(new TestQueryA(id));
            result.ShouldBe(id);
        }
    }
}