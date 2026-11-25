// Licensed under the MIT License.
// Copyright (c) .NET Foundation and Contributors.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Paramore.Darker;
using Paramore.Darker.Policies.Handlers;

namespace Paramore.Darker.Tests.AOT.Scenarios
{
    /// <summary>
    /// Issues #409 and #410 — under native AOT with full trimming, a <c>[FallbackPolicy]</c> query whose handler
    /// throws must run the handler's <c>Fallback</c> / <c>FallbackAsync</c>, on both the sync and the async
    /// pipeline. Returns 0 when both pipelines return the fallback's result; returns 1 otherwise.
    /// </summary>
    internal static class Case3FallbackRunsWhenOnlyExecuteIsRooted
    {
        // Root ONLY Execute / ExecuteAsync — the methods PipelineBuilder's documented AOT known-limitation
        // asks consumers to compensate for. Do NOT root PublicMethods: that would also keep Fallback /
        // FallbackAsync and hide #409, where the pipeline's own fallback lookup is not rooted.
        [DynamicDependency(nameof(AotFallbackQueryHandler.Execute), typeof(AotFallbackQueryHandler))]
        [DynamicDependency(nameof(AotFallbackQueryHandlerAsync.ExecuteAsync), typeof(AotFallbackQueryHandlerAsync))]
        [DynamicDependency(nameof(DirectFallbackQueryHandler.Execute), typeof(DirectFallbackQueryHandler))]
        [DynamicDependency(nameof(DirectFallbackQueryHandlerAsync.ExecuteAsync), typeof(DirectFallbackQueryHandlerAsync))]
        public static async Task<int> RunAsync()
        {
            var result = await RunAsync<AotFallbackQueryHandler, AotFallbackQueryHandlerAsync>();
            if (result != 0)
                return result;

            return await RunAsync<DirectFallbackQueryHandler, DirectFallbackQueryHandlerAsync>();
        }

        private static async Task<int> RunAsync<TSync, TAsync>()
            where TSync : IQueryHandler<AotFallbackQuery, AotFallbackQuery.Result>, new()
            where TAsync : IQueryHandlerAsync<AotFallbackQuery, AotFallbackQuery.Result>, new()
        {
            // Arrange
            var registry = new QueryHandlerRegistry();
            registry.Register<AotFallbackQuery, AotFallbackQuery.Result, TSync>();
            var asyncRegistry = new QueryHandlerRegistryAsync();
            asyncRegistry.Register<AotFallbackQuery, AotFallbackQuery.Result, TAsync>();

            var handlerFactory = new SimpleHandlerFactory(handlerType => handlerType == typeof(TSync)
                ? new TSync()
                : new TAsync());
            var decoratorFactory = new SimpleHandlerDecoratorFactory(decoratorType =>
                decoratorType == typeof(FallbackPolicyDecorator<IQuery<AotFallbackQuery.Result>, AotFallbackQuery.Result>)
                    ? new FallbackPolicyDecorator<IQuery<AotFallbackQuery.Result>, AotFallbackQuery.Result>()
                    : new FallbackPolicyDecoratorAsync<IQuery<AotFallbackQuery.Result>, AotFallbackQuery.Result>());

            var configuration = new HandlerConfiguration(
                registry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory,
                asyncRegistry, handlerFactory, new InMemoryDecoratorRegistry(), decoratorFactory);
            var processor = new QueryProcessor(configuration, new InMemoryQueryContextFactory());
            var queryId = Guid.NewGuid();

            // Act
            AotFallbackQuery.Result syncResult, asyncResult;
            try
            {
                syncResult = processor.Execute(new AotFallbackQuery(queryId));
                asyncResult = await processor.ExecuteAsync(new AotFallbackQuery(queryId));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Case3] FAIL: fallback query threw {ex.GetType().Name}: {ex.Message}");
                return 1;
            }

            // Assert — only the fallbacks return the query's id
            if (syncResult.Value != queryId || asyncResult.Value != queryId)
            {
                Console.Error.WriteLine(
                    $"[Case3] FAIL: expected fallback value {queryId}; sync returned {syncResult.Value}, async returned {asyncResult.Value}.");
                return 1;
            }

            Console.WriteLine($"[Case3] PASS: {typeof(TSync).Name} and {typeof(TAsync).Name} ran their fallbacks with only Execute rooted.");
            return 0;
        }
    }
}
