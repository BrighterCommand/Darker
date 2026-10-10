# Release Notes

Unreleased changes are collected under `## Master` and copied into the GitHub release when a version
is tagged. Each spec that changes behaviour or a public interface gets one section, marked with a
`<!-- spec: {spec directory} -->` line beneath its heading; `/spec:write_release_notes` writes and
refreshes those sections.

## Master

### Share the request scope with Brighter and harden pipeline teardown (spec 015, #377)
<!-- spec: 015-di_lifetime_conformance -->

A Darker query handler can now resolve the same Scoped services, such as an EF Core `DbContext`, as the controller and Brighter command handlers in one ASP.NET Core request: register Scoped handlers and call `AddDarkerRequestScope()` from the new `Paramore.Darker.Extensions.AspNetCore` package. Making the `QueryProcessor` Scoped never achieved this, and the guidance is corrected. Pipeline teardown now releases every component and disposes the per-query scope even when a `Dispose` throws, and it no longer hides the handler's own exception.

#### Breaking changes

- `IQueryHandlerFactoryAsync.Release` and `IQueryHandlerDecoratorFactoryAsync.Release` become `ValueTask ReleaseAsync`, and `IAmALifetime` now extends `IAsyncDisposable`. *(source and binary)* Custom implementations must implement `ReleaseAsync` and `DisposeAsync` instead.
- Pipeline teardown no longer throws: a failing release or `Dispose` is logged at Error, and the caller sees the query's result or the handler's own exception. *(behavioural)* Watch logs for teardown faults instead of catching them.
- On `Execute`, a Scoped service that implements only `IAsyncDisposable` is still not disposed, but the fault is now logged instead of thrown. *(behavioural)* Use `ExecuteAsync` for such services.
- On `ExecuteAsync` and `ExecuteStream`, the handler is released by the async handler factory that created it, not the sync factory. *(behavioural)* Custom async handler factories must release their own handlers in `ReleaseAsync`.
- `IQueryHandlerFactory` and `IQueryHandlerFactoryAsync` gain `IAmAScope? CreatePipelineScope()`. *(source and binary)* Custom handler factories must implement it, returning null when they create no per-query scope.
- `IAmALifetime.Add(IDisposable)` is removed, and `IAmALifetime` gains `PipelineScope`. *(source and binary)* Return per-query disposables inside the one `IAmAScope` handle from `CreatePipelineScope()` instead of calling `Add`.
- A Scoped or Transient component requested without a recognised pipeline scope now throws `ConfigurationException` instead of silently creating a scope. *(behavioural)* Pair a container-backed decorator factory with a handler factory that returns a scope.
