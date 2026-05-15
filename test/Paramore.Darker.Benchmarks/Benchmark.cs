using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Paramore.Darker.Builder;

namespace Paramore.Darker.Benchmarks
{
    public class Benchmark
    {
        private readonly IQueryProcessor _queryProcessor;
        private readonly IQueryProcessorAsync _queryProcessorAsync;

        public Benchmark()
        {
            var handlerRegistry = new QueryHandlerRegistry();
            handlerRegistry.Register<BasicSyncQuery, bool, BasicSyncQueryHandler>();
            handlerRegistry.Register<BasicAsyncQuery, bool, BasicAsyncQueryHandler>();

            var processor = QueryProcessorBuilder.With()
                .Handlers(handlerRegistry, t => (IQueryHandler)Activator.CreateInstance(t), t => {}, t => (IQueryHandlerDecorator)Activator.CreateInstance(t))
                .InMemoryQueryContextFactory()
                .Build();

            _queryProcessor = (IQueryProcessor)processor;
            _queryProcessorAsync = (IQueryProcessorAsync)processor;
        }

        [Benchmark]
        public void BasicSyncQuery()
        {
            _queryProcessor.Execute(new BasicSyncQuery());
        }

        [Benchmark]
        public async Task BasicAsyncQuery()
        {
            await _queryProcessorAsync.ExecuteAsync(new BasicAsyncQuery());
        }
    }
}