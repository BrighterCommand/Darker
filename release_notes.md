# Release Notes

Unreleased changes are collected under `## Master` and copied into the GitHub release when a version
is tagged. Each spec that changes behaviour or a public interface gets one section, marked with a
`<!-- spec: {spec directory} -->` line beneath its heading; `/spec:write_release_notes` writes and
refreshes those sections.

## Master

### Share the request scope with Brighter and harden pipeline teardown (spec 015, #377)
<!-- spec: 015-di_lifetime_conformance -->

A Darker query handler can now resolve the same Scoped services, such as an EF Core `DbContext`, as the controller and Brighter command handlers in one ASP.NET Core request: register Scoped handlers and call `AddDarkerRequestScope()` from the new `Paramore.Darker.Extensions.AspNetCore` package. Making the `QueryProcessor` Scoped never achieved this, and the guidance is corrected. Pipeline teardown now releases every component and disposes the per-query scope even when a release throws, and it no longer hides the handler's own exception.

#### Breaking changes

- `IQueryHandlerFactoryAsync.Release` and `IQueryHandlerDecoratorFactoryAsync.Release` become `ValueTask ReleaseAsync`, and `IAmALifetime` now extends `IAsyncDisposable`. *(source and binary)* Custom implementations must implement `ReleaseAsync` and `DisposeAsync` instead.
- Pipeline teardown no longer throws: a failing release or `Dispose` is logged at Error, and the caller sees the query's result or the handler's own exception. *(behavioural)* Watch logs for teardown faults instead of catching them.
- On `Execute`, a Scoped service that implements only `IAsyncDisposable` is still not disposed, but the fault is now logged instead of thrown. *(behavioural)* Use `ExecuteAsync` for such services.
- On `ExecuteAsync` and `ExecuteStream`, the handler is released by the async handler factory that created it, not the sync factory. *(behavioural)* Custom async handler factories must release their own handlers in `ReleaseAsync`.
- On `ExecuteAsync` and `ExecuteStream`, the Simple factories now await `DisposeAsync` on a component that implements `IAsyncDisposable`; before, such a component received `Dispose`. *(behavioural)* Make `DisposeAsync` do the clean-up your component's `Dispose` did.
- A build that fails part-way now releases the decorators it already created, so the Simple decorator factory disposes them. *(behavioural)* Make decorator `Dispose` safe to call after a failed build.
- `IQueryHandlerFactory` and `IQueryHandlerFactoryAsync` gain `IAmAScope? CreatePipelineScope()`. *(source and binary)* Custom handler factories must implement it, returning null when they create no per-query scope.
- `IAmALifetime.Add(IDisposable)` is removed, and `IAmALifetime` gains `PipelineScope`. *(source and binary)* Return per-query disposables inside the one `IAmAScope` handle from `CreatePipelineScope()` instead of calling `Add`; a custom decorator factory disposes its own per-query resources in `Release` or `ReleaseAsync`.
- A custom container-backed decorator factory must resolve from `lifetime.PipelineScope`, which is null when the handler factory creates no scope. *(behavioural)* Throw `ConfigurationException` when it is null or unrecognised, rather than creating a scope.
- With a Scoped or Transient `HandlerLifetime`, the decorators Darker registers are Transient, so each query gets its own instances; a decorator type you register yourself keeps your registration. *(behavioural)* Do not rely on one decorator instance being shared across a scope.

#### Added

- `Paramore.Darker.Extensions.AspNetCore`, with `services.AddDarkerRequestScope(ScopeAffinity affinity = ScopeAffinity.JoinAmbient)`.
- `IAmAScope`, `IAmAScopeProvider`, `ScopeAffinity` and `AmbientScopeSourceException` in `Paramore.Darker`.
- `IAmAServiceProviderScope`, `DarkerOptions.DefaultScopeAffinity` (default `AlwaysNew`) and `AddDarkerScopeProvider<TProvider>(affinity)` in `Paramore.Darker.Extensions.DependencyInjection`.
- Under `JoinAmbient`, queries in one ambient scope share its Scoped services, including a Scoped handler instance of the same handler type. Concurrent queries in one request need `AlwaysNew`.
