# Release Notes

Unreleased changes are collected under `## Master` and copied into the GitHub release when a version
is tagged. Each spec that changes behaviour or a public interface gets one section, marked with a
`<!-- spec: {spec directory} -->` line beneath its heading; `/spec:write_release_notes` writes and
refreshes those sections.

## Master

### Validate query pipelines when the host starts (spec 016, #305)
<!-- spec: 016-pipeline_validation -->

`ValidatePipelines()` is a new opt-in on the `AddDarker` chain. When the host starts, it checks every registered handler and its decorator pipeline, and reports all problems at once instead of one at a time on the first query: a missing or non-public handler, a handler on the wrong interface, a decorator attribute on the wrong path, an unregistered handler or decorator, a duplicate `Step`, and a fallback placed inside a retry. It also checks lifetimes and scopes: `InertScopeAffinity`, `CaptiveDependency` and `DuplicateScopeProvider`. Errors stop the host unless you pass `throwOnError: false`. Call it last, after `AddDarker`, handler registration, the scope provider and your handlers' dependencies, because the lifetime rules read a snapshot taken at that call. `provider.ValidateAll()` runs every validator on demand and returns one combined result, for a test or a health check. No existing signature changed, and nothing changes for an application that does not call `ValidatePipelines()`. The README lists each rule with its cause and fix.

#### Breaking changes

- `Paramore.Darker.Extensions.DependencyInjection` now references `Microsoft.Extensions.Hosting.Abstractions`, and so its transitive abstractions, even if you never opt in. *(compatibility)* Check for version conflicts with your own hosting packages.
- An internal handler that you register explicitly still runs, but fails validation once you call `ValidatePipelines()`. *(behavioural)* Make the handler public.

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
