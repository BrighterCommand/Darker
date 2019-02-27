using System;
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

            var queryProcessor = QueryProcessorBuilder.With()
                .SimpleInjectorHandlers(container, opts =>
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