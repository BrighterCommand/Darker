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
**EF Core and the request scope.** EF Core registers the `DbContext` as Scoped. A Scoped `QueryProcessor` gets a sibling scope, so it does **not** share the request's `DbContext`. To share it, use Scoped handlers and opt in to the request scope with the `Paramore.Darker.Extensions.AspNetCore` package:
```csharp
services.AddDarker(options => options.HandlerLifetime = ServiceLifetime.Scoped)
    .AddHandlersFromAssemblies(typeof(MyHandler).Assembly);
services.AddDarkerRequestScope();    // Paramore.Darker.Extensions.AspNetCore
services.AddBrighterRequestScope();  // Brighter's own opt-in, if you also use Brighter
```
The two opt-in calls are independent. Queries in one request share its Scoped services, so concurrent queries in one request need `ScopeAffinity.AlwaysNew`.

This example uses the query logging and policy decorators, which are built into the core
[Paramore.Darker](https://www.nuget.org/packages/Paramore.Darker) package.
Have a look at [Program.cs](https://github.com/BrighterCommand/Darker/blob/master/samples/SampleMinimalApi/Program.cs)
in the [SampleMinimalApi](https://github.com/BrighterCommand/Darker/tree/master/samples/SampleMinimalApi) project for more examples on how to use the integrations.

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
using Paramore.Darker.Logging.Attributes;
using Paramore.Darker.Policies.Attributes;
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

## Validating pipelines at startup
A handler with a typo in its registration, a decorator that was never registered, or a `[RetryableQuery]` wrapped the wrong way round fails on the first query, or never reports at all. `ValidatePipelines()` checks every registered handler and its decorator pipeline when the host starts, so these problems stop the host instead of surfacing in production. It is opt-in and lives in the [Paramore.Darker.Extensions.DependencyInjection](https://www.nuget.org/packages/Paramore.Darker.Extensions.DependencyInjection) package.

```csharp
services.AddDarker()
    .AddHandlersFromAssemblies(typeof(GetPeopleQueryHandler).Assembly)
    .AddJsonQueryLogging()
    .AddDefaultPolicies()
    .ValidatePipelines();   // call it last
```

`ValidatePipelines` takes `throwOnError`, which is `true` by default. With `throwOnError: true` an error finding throws a `PipelineValidationException` that stops the host. With `ValidatePipelines(throwOnError: false)` errors are logged at Error and the host starts. Warnings never stop the host. Calling it more than once registers validation once, and the last call sets `throwOnError`.

Call `ValidatePipelines()` last, after the other `AddDarker` calls and your handler registrations. Pipeline findings do not depend on the order, because the validator reads the live registries, but the call order matches Brighter's `ValidatePipelines()`.

Validation reads registrations and handler and decorator types. It never creates a handler, a decorator or a scope.

### When it runs
Validation runs in the starting phase of the host (`IHostedLifecycleService.StartingAsync`). The host runs every `StartingAsync` to completion before any `StartAsync`, and the ASP.NET Core server starts in its own `StartAsync`. So the guarantee is that validation runs **before the server**, not that it runs before any query. It has three limits:
- A hosted service whose constructor runs a query runs before validation, because the host constructs every hosted service first.
- Another hosted service whose `StartingAsync` runs queries can run before or beside validation. Registration order decides under sequential start, and nothing decides when `HostOptions.ServicesStartConcurrently` is true.
- A host built on an older hosting library never calls the starting phase. There, validation runs from `StartAsync` in registration order, and the ordering guarantee is absent. It still runs only once.

Warnings are logged before any exception is thrown. Each finding is logged as `Pipeline validation [{RuleId}] {Source}: {Message}`. A run with no findings logs `Pipeline validation found no problems` at Debug.

### Rules
| Rule | Severity | Cause | Fix |
|------|----------|-------|-----|
| `HandlerRegistered` | Error | A query is registered without a handler type. | Register a handler for the query. |
| `HandlerPublic` | Error | The handler type is not public. | Make the handler public. |
| `HandlerMatchesPath` | Error | The handler does not implement the interface of its registry (sync, async or stream), or implements it only explicitly, so there is no public method to attribute. | Implement the matching interface, with a public method. |
| `DecoratorAttributeKind` | Error | The handler method carries decorator attributes that belong to another path, for example a sync attribute on an async method. | Use the attribute that matches the path named in the message. |
| `PipelineDescribable` | Error | The pipeline of a handler cannot be read, for example an attribute cannot be instantiated. | Fix the attribute or handler named in the message. |
| `HandlerResolvable` | Error | The handler type is not registered with the container. | Register the handler. |
| `DecoratorResolvable` | Error | A decorator type used by a pipeline is not registered with the container. | Register the decorator. |
| `DuplicateStep` | Warning | Two attributes on one method share a `Step`, so their order is undefined. | Give each attribute its own `Step`. |
| `DecoratorOrdering` | Warning | Two decorators are in an order that defeats one of them. The built-in rule flags a fallback policy placed inside a retry, where the fallback hides the failure and the retry never runs. | Reorder the `Step` values. |
| `RegistryNotInspectable` | Warning | A custom handler registry does not implement `IAmAHandlerRegistryInspector`, so its handlers are not checked. | Implement the interface, or accept that those handlers are unchecked. |
| `ResolvabilityNotChecked` | Warning | The validator has no resolvability probe, so `HandlerResolvable` and `DecoratorResolvable` were skipped. | Supply an `IAmAComponentResolvabilityProbe`. `ValidatePipelines()` supplies one. |
| `RuleEvaluationFailed` | Error | A rule, or a registry read, threw while validating. | Read the exception message in the finding and fix the registry or the rule. |

Resolvability asks the container whether a type is registered. It does not construct it, so a registered handler with a missing constructor dependency passes. The container's own `ValidateOnBuild` option (`new ServiceProviderOptions { ValidateOnBuild = true }`) reports that case.

### Step order
The position meaning of `Step` is the one to watch. The **lowest `Step` is outermost**: it runs first on the way in and last on the way out. `[FallbackPolicy(1)]` with `[RetryableQuery(2)]` puts the fallback outside the retry, which is the order the `DecoratorOrdering` rule wants.

To add your own constraint, register a rule beside the built-in ones:
```csharp
services.AddSingleton(new DecoratorOrderingRule(
    typeof(OuterDecoratorAsync<,>),
    typeof(InnerDecoratorAsync<,>),
    new ValidationRuleId("OuterWrapsInner"),
    "the inner decorator must not see the outer decorator's failures."));
```
The outer and inner types are open generic decorator types. `ValidationRuleId` is in `Paramore.Darker.Specifications` and `DecoratorOrderingRule` is in `Paramore.Darker.PipelineValidation`.

### Validating on demand
`ValidateAll` on `IServiceProvider` runs every registered `IAmAPipelineValidator` and returns one combined `PipelineValidationResult`. It never throws for a finding. Use it in a test or a health check:
```csharp
using Paramore.Darker.Extensions.DependencyInjection;

PipelineValidationResult result = provider.ValidateAll();
result.ThrowIfInvalid();   // optional: throws PipelineValidationException when there are errors
```
Do not resolve a single `IAmAPipelineValidator` for this. A single resolve returns only the last registered validator, so it sees one validator rather than all of them.

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

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for our coding guidelines, how to build and test, and how to submit a change.

We welcome contributions authored with a coding agent. See [AGENTIC_CODING.md](AGENTIC_CODING.md) for how to use our agent instructions and slash commands — the spec and bugfix workflows, review gears, and keeping state across sessions — and remember that you remain responsible for the code you submit.
