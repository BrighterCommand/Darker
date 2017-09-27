using System;
using LightInject;
using Paramore.Darker.Builder;
using Paramore.Darker.LightInject;
using Paramore.Darker.Testing.Ports;
using Shouldly;
using Xunit;

namespace Paramore.Darker.Tests.Integrations
{
    public class LightInjectTests
    {
        [Fact]
        public void HandlersGetWiredWithLightInject()
        {
            var container = new ServiceContainer();

            var queryProcessor = QueryProcessorBuilder.With()
                .LightInjectHandlers(container, opts =>
                    opts.WithQueriesAndHandlersFromAssembly(typeof(TestQueryHandler).Assembly))
                .InMemoryQueryContextFactory()
                .Build();

            container.RegisterInstance(queryProcessor);

            var resolvedQueryProcessor = container.GetInstance<IQueryProcessor>();
            resolvedQueryProcessor.ShouldNotBeNull();

            var id = Guid.NewGuid();
            var result = resolvedQueryProcessor.Execute(new TestQueryA(id));
            result.ShouldBe(id);
        }
    }
}