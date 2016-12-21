# Darker
The query-side counterpart of [Brighter](https://github.com/iancooper/Paramore).

[![Build status](https://ci.appveyor.com/api/projects/status/6h8wjaxfd01aw14h?svg=true)](https://ci.appveyor.com/project/dstockhammer/darker)

**This project is in a very early alpha stage. Use with caution!**

## Usage

Register your queries and handlers with `QueryHandlerRegistry` and use `QueryProcessorBuilder` to configure and build a `IQueryProcessor`.

```csharp
var registry = new QueryHandlerRegistry();
registry.Register<FooQuery, FooQuery.Result, FooQueryHandler>();

IQueryProcessor queryProcessor = QueryProcessorBuilder.With()
    .Handlers(registry, Activator.CreateInstance, Activator.CreateInstance)
    .DefaultPolicies()
    .InMemoryRequestContextFactory()
    .Build();
```

Instead of `Activator.CreateInstance`, you can pass any factory `Func<Type, object>` to constuct handlers and decorator. Usually this calls your IoC container.
Inject `IQueryProcessor` and call `Execute` or `ExecuteAsync` to dispatch your query to the registered query handler.

```csharp
using Darker;
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
        return Ok(result);
    }
}
```

```csharp
using Darker;

public sealed class FooQuery : IQueryRequest<FooQuery.Result>
{
    public int Number { get; }

    public FooQuery(int number)
    {
        Number = number;
    }

    public sealed class Result : IQueryResponse
    {
        public string Answer { get; }

        public Result(string answer)
        {
            Answer = answer;
        }
    }
}
```

Implement either `QueryHandler<,>` or `AsyncQueryHandler<,>` depending on whether you wish to execute your queries synchronously or asynchronously.
For most control, you can also implement `IQueryHandler<,>` directly.

```csharp
using Darker;
using System.Threading;
using System.Threading.Tasks;

public sealed class FooQueryHandler : AsyncQueryHandler<FooQuery, FooQuery.Result>
{
    [RequestLogging(1)]
    public override async Task<FooQuery.Result> ExecuteAsync(FooQuery query, CancellationToken cancellationToken = default(CancellationToken))
    {
        var answer = await CalculateAnswerForNumber(query.Number, cancellationToken).ConfigureAwait(false);
        return new FooQuery.Result(answer);
    }
}
```
