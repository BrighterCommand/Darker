# Darker
The query-side counterpart of [Brighter](https://github.com/BrighterCommand/Paramore.Brighter).

![.NET Core](https://github.com/BrighterCommand/Darker/workflows/.NET%20Core/badge.svg)
[![NuGet](https://img.shields.io/nuget/v/Paramore.Darker.svg)](https://www.nuget.org/packages/Paramore.Darker)

## Usage with Microsoft.Extensions.DependencyInjection
In your `ConfigureServices` method, use `AddDarker` to add Darker to the container.
DI integration is provided by the [Paramore.Darker.Extensions.DependencyInjection](https://www.nuget.org/packages/Paramore.Darker.Extensions.DependencyInjection) package.

```csharp
// This method gets called by the runtime. Use this method to add services to the container.
public void ConfigureServices(IServiceCollection services)
{
    // Add Darker and some extensions.
    services.AddDarker()
        .AddHandlersFromAssemblies(typeof(GetPeopleQueryHandler).Assembly)
        .AddJsonQueryLogging()
        .AddDefaultPolicies();

    // Add framework services.
    services.AddMvc();
}
```
**WARNING** if you are using EFCore the DBContext DI Lifetime is scoped, for Darker to play nicely with EFCore and DI the QueryProcessor must also be registration as Scoped
```csharp
 services.AddDarker(options =>
                {
                    //EFCore by default registers Context as scoped, which forces the QueryProcessorLifetime to also be scoped
                    options.QueryProcessorLifetime = ServiceLifetime.Scoped;
                })
```

This example uses the request logging integration provided by [Paramore.Darker.QueryLogging](https://www.nuget.org/packages/Paramore.Darker.QueryLogging)
and policy integration provided by [Paramore.Darker.Policies](https://www.nuget.org/packages/Paramore.Darker.Policies).
Have a look at the [Startup.ConfigureServices](https://github.com/BrighterCommand/Darker/blob/master/samples/SampleApi/Startup.cs) method
in the [SampleApi](https://github.com/BrighterCommand/Darker/tree/master/samples/SampleApi) project for more examples on how to use the integrations.

Inject `IQueryProcessor` and call `Execute` or `ExecuteAsync` to dispatch your query to the registered query handler.

```csharp
using Paramore.Darker;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

public class FooController : ControllerBase
{
    private readonly IQueryProcessor _queryProcessor;

    public FooController(IQueryProcessor queryProcessor)
    {
        _queryProcessor = queryProcessor;
    }

    public async Task<IActionResult> Get(CancellationToken cancellationToken = default(CancellationToken))
    {
        var query = new GetFoo(42);
        var result = await _queryProcessor.ExecuteAsync(query, cancellationToken);
        return Ok(result);
    }
}
```

```csharp
using Paramore.Darker;

public sealed class GetFoo : IQuery<string>
{
    public int Number { get; }

    public GetFoo(int number)
    {
        Number = number;
    }
}
```

Implement either `QueryHandler<,>` or `QueryHandlerAsync<,>` depending on whether you wish to execute your queries synchronously or asynchronously.
For most control, you can also implement `IQueryHandler<,>` directly.

```csharp
using Paramore.Darker;
using Paramore.Darker.Attributes;
using Paramore.Darker.Policies;
using Paramore.Darker.QueryLogging;
using System.Threading;
using System.Threading.Tasks;

public sealed class GetFooHandler : QueryHandlerAsync<GetFoo, string>
{
    [QueryLogging(1)]
    [FallbackPolicy(2)]
    [RetryableQuery(3)]
    public override async Task<string> ExecuteAsync(GetFoo query, CancellationToken cancellationToken = default(CancellationToken))
    {
        return await FetchFooForNumber(query.Number, cancellationToken);
    }
}
```

## Usage without ASP.NET
Register your queries and handlers with `QueryHandlerRegistry` and use `QueryProcessorBuilder` to configure and build a `IQueryProcessor`.

```csharp
var registry = new QueryHandlerRegistry();
registry.Register<GetFoo, string, GetFooHandler>();

IQueryProcessor queryProcessor = QueryProcessorBuilder.With()
    .Handlers(registry, Activator.CreateInstance, t => {}, Activator.CreateInstance)
    .InMemoryQueryContextFactory()
    .Build();
```

Instead of `Activator.CreateInstance`, you can pass any factory `Func<Type, object>` to construct handlers and decorators.

> **Note:** The `Paramore.Darker.SimpleInjector` and `Paramore.Darker.LightInject` packages have been removed as of V5. If you use a third-party DI container, use its built-in adapter for `Microsoft.Extensions.DependencyInjection` and integrate with Darker via the `Paramore.Darker.Extensions.DependencyInjection` package instead.

## Streaming Queries

Darker supports streaming queries that yield results incrementally as `IAsyncEnumerable<TResult>`,
so large result sets or real-time feeds are produced on demand rather than buffered into memory.

### Define a stream query and handler

```csharp
using Paramore.Darker;
using System.Collections.Generic;
using System.Threading;

// TResult is the item type, not the enumerable.
public sealed class GetOrdersStream : IStreamQuery<Order>
{
    public string CustomerId { get; }
    public GetOrdersStream(string customerId) => CustomerId = customerId;
}

public sealed class GetOrdersStreamHandler : StreamQueryHandler<GetOrdersStream, Order>
{
    public override async IAsyncEnumerable<Order> ExecuteAsync(
        GetOrdersStream query,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var order in _repository.StreamByCustomerAsync(query.CustomerId, cancellationToken))
            yield return order;
    }
}
```

### Execute with `await foreach`

```csharp
await foreach (var order in queryProcessor.ExecuteStream(new GetOrdersStream("C42"), cancellationToken))
{
    // items arrive as the handler produces them — no buffering
    Process(order);
}
```

### Registration with DI (assembly scan)

`AddHandlersFromAssemblies` picks up `IStreamQueryHandler<,>` implementations automatically alongside
sync and async handlers:

```csharp
services.AddDarker()
    .AddHandlersFromAssemblies(typeof(GetOrdersStreamHandler).Assembly);
```

### Registration with DI (explicit)

```csharp
services.AddDarker()
    .AddStreamHandlers(r => r.Register<GetOrdersStream, Order, GetOrdersStreamHandler>());
```

### Registration without DI

```csharp
var streamRegistry = new StreamQueryHandlerRegistry();
streamRegistry.Register<GetOrdersStream, Order, GetOrdersStreamHandler>();

IQueryProcessor queryProcessor = QueryProcessorBuilder.With()
    .Handlers(new HandlerConfiguration(
        syncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
        asyncRegistry, handlerFactory, decoratorRegistry, decoratorFactory,
        streamRegistry))
    .InMemoryQueryContextFactory()
    .Build();
```

### Resilience (Polly v8)

Use `[UseResiliencePipelineStream]` — **not** `[RetryableQuery]` or `[FallbackPolicy]`, which apply
only to single-result handlers and throw a `ConfigurationException` on a stream handler:

```csharp
public sealed class GetOrdersStreamHandler : StreamQueryHandler<GetOrdersStream, Order>
{
    [UseResiliencePipelineStream(1, "MyRetryPipeline")]
    public override async IAsyncEnumerable<Order> ExecuteAsync(GetOrdersStream query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    { ... }
}
```

Resilience covers **stream establishment and the first item only**. Once the first item has been
yielded to the caller, the pipeline has exited and subsequent faults propagate un-retried. A `Timeout`
strategy therefore bounds *getting the stream started*, not total enumeration time. `Hedging` is not
supported for streams. These are intentional semantics, not limitations to be worked around.

### Documented semantics

| Behaviour | Detail |
|---|---|
| **Laziness** | The framework never buffers the sequence; items are produced on demand. Custom decorators must also avoid buffering (e.g. `ToListAsync`). |
| **Cancellation** | Pass a `CancellationToken` to `ExecuteStream`; cancelling mid-stream stops enumeration and propagates `OperationCanceledException`. |
| **Exceptions mid-stream** | Faults during enumeration propagate with their original stack trace — no `TargetInvocationException` wrapper. |
| **Configuration errors** | A missing or mismatched handler surfaces as `ConfigurationException` from the caller's **first `await foreach` iteration**, not from the `ExecuteStream` call itself (deliberate — resolving eagerly would leak the handler if the caller never enumerates). |
| **Re-enumeration** | Each `await foreach` over the same `IAsyncEnumerable` re-executes the handler with a fresh pipeline. The stream is cold, not cached. To iterate twice over the same data, buffer it yourself (`await ToListAsync()`). |
| **Caller-supplied context** | A `queryContext` passed to `ExecuteStream` is scoped to a **single enumeration**. Concurrent or repeated enumeration is only safe when the processor creates the context (pass `null`). |
| **Legacy attributes** | `[RetryableQuery]` and `[FallbackPolicy]` do **not** apply to streams. Use `[UseResiliencePipelineStream]` for stream resilience. Applying a mismatched attribute throws `ConfigurationException`. |
