# Darker
The query-side counterpart of [Brighter](https://github.com/BrighterCommand/Paramore.Brighter).

[![Build status](https://ci.appveyor.com/api/projects/status/almoys73cgc7gs8n?svg=true)](https://ci.appveyor.com/project/BrighterCommand/darker)
[![NuGet](https://img.shields.io/nuget/v/Paramore.Darker.svg)](https://www.nuget.org/packages/Paramore.Darker)

## Usage with ASP.NET Core
In your `ConfigureServices` method, use `AddDarker` to add Darker to the container.
ASP.NET Core integration is provided by the [Paramore.Darker.AspNetCore](https://www.nuget.org/packages/Paramore.Darker.AspNetCore) package.

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
        var query = new FooQuery();
        var result = await _queryProcessor.ExecuteAsync(query, cancellationToken);

        return Ok(result.Answer);
    }
}
```

```csharp
using Paramore.Darker;

public sealed class FooQuery : IQuery<FooQuery.Result>
{
    public int Number { get; }

    public FooQuery(int number)
    {
        Number = number;
    }

    public sealed class Result
    {
        public string Answer { get; }

        public Result(string answer)
        {
            Answer = answer;
        }
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

public sealed class FooQueryHandler : QueryHandlerAsync<FooQuery, FooQuery.Result>
{
    [QueryLogging(1)]
    [FallbackPolicy(2)]
    [RetryableQuery(3)]
    public override async Task<FooQuery.Result> ExecuteAsync(FooQuery query, CancellationToken cancellationToken = default(CancellationToken))
    {
        var answer = await CalculateAnswerForNumber(query.Number, cancellationToken).ConfigureAwait(false);
        return new FooQuery.Result(answer);
    }
}
```

## Usage without ASP.NET
Register your queries and handlers with `QueryHandlerRegistry` and use `QueryProcessorBuilder` to configure and build a `IQueryProcessor`.

```csharp
var registry = new QueryHandlerRegistry();
registry.Register<FooQuery, FooQuery.Result, FooQueryHandler>();

IQueryProcessor queryProcessor = QueryProcessorBuilder.With()
    .Handlers(registry, Activator.CreateInstance, t => {}, Activator.CreateInstance)
    .InMemoryQueryContextFactory()
    .Build();
```

Instead of `Activator.CreateInstance`, you can pass any factory `Func<Type, object>` to constuct handlers and decorator.
Integrations with some DI frameworks are available, for example [SimpleInjector](https://simpleinjector.org), as provided by the [Paramore.Darker.SimpleInjector](https://www.nuget.org/packages/Paramore.Darker.SimpleInjector) package:

```csharp
var container = new Container();

var queryProcessor = QueryProcessorBuilder.With()
    .SimpleInjectoHandlers(container, opts =>
        opts.WithQueriesAndHandlersFromAssembly(typeof(GetPeopleQuery).Assembly))
    .InMemoryQueryContextFactory()
    .Build();

container.Register<IQueryProcessor>(queryProcessor);
```

In this case you don't need to manually register queries or handlers as the integration allows scanning assemblies for matching types.