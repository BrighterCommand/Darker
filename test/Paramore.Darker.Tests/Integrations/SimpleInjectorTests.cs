using System;
using Microsoft.Extensions.Logging;
using Paramore.Darker.Builder;
using Paramore.Darker.SimpleInjector;
using Paramore.Darker.Testing.Ports;
using Shouldly;
using SimpleInjector;
using Xunit;

namespace Paramore.Darker.Tests.Integrations
{
    public class SimpleInjectorTests
    {
        [Fact]
        public void HandlersGetWiredWithSimpleInjectors()
        {
            var container = new Container();

            ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            {
                //builder.AddConsole();
                //builder.AddDebug();
            });

            container.RegisterInstance<ILoggerFactory>(loggerFactory);

            var queryProcessor = QueryProcessorBuilder.With()
                .SimpleInjectorHandlers(container, opts =>
                    opts.WithQueriesAndHandlersFromAssembly(typeof(TestQueryHandler).Assembly))
                .InMemoryQueryContextFactory()
                .Build();
            
            container.RegisterInstance(queryProcessor);
            
            container.Verify();
            
            var resolvedQueryProcessor = container.GetInstance<IQueryProcessor>();
            resolvedQueryProcessor.ShouldNotBeNull();

            var id = Guid.NewGuid();
            var result = resolvedQueryProcessor.Execute(new TestQueryA(id));
            result.ShouldBe(id);
        }
    }
}