# V5 Release Notes Draft — Query Logging now uses `System.Text.Json`

> **Status**: draft artefact for the V5 GitHub release notes. Tracked here (`specs/006-json_serializer/release-notes-draft.md`) so the Definition-of-Done release-notes requirement is auditable within issue #294 / ADR 0012, rather than deferred to release-tagging. Copy/reference the relevant sections into the GitHub release at tagging time. (The repo has no `CHANGELOG.md`, so release notes are the canonical channel.)

## Summary

The built-in query logging decorator now serialises the `{Query}` log argument with `System.Text.Json` instead of `Newtonsoft.Json`. `Newtonsoft.Json` is **removed** as a dependency of `Paramore.Darker`. This brings Darker into parity with Brighter (which migrated to `System.Text.Json` in [#1470](https://github.com/BrighterCommand/Brighter/pull/1470)) and removes a transitive dependency that ADR 0011 had pushed onto every consumer.

This is a **breaking change** for consumers who customised the JSON output of the logging decorator. The breaks and their migrations are below.

---

## Breaking changes

### 1. Configuration callback type changed: `Action<JsonSerializerSettings>` → `Action<JsonSerializerOptions>`

The `AddJsonQueryLogging` / `JsonQueryLogging` configuration callback now hands you a `System.Text.Json.JsonSerializerOptions` instead of a `Newtonsoft.Json.JsonSerializerSettings`. Any converter registration or formatting tweak must be re-expressed against the `System.Text.Json` API surface.

### 2. Decorator constructor no longer takes a serialiser parameter

`QueryLoggingDecorator<,>` and `QueryLoggingDecoratorAsync<,>` no longer accept a serialiser/settings constructor parameter. They read the process-global `Paramore.Darker.Logging.QueryLoggingJsonOptions.Options` at serialize time (`JsonSerializer.Serialize(value, value.GetType(), QueryLoggingJsonOptions.Options)`). If you constructed these decorators directly (rare — normally they are resolved by the pipeline), drop the argument.

### 3. No DI singleton is registered for serializer settings

`AddJsonQueryLogging(...)` no longer performs `services.AddSingleton<JsonSerializerSettings>(...)` (nor any equivalent for `JsonSerializerOptions`). Configuration flows through the static `QueryLoggingJsonOptions.Options` holder, applied via the optional `configure` callback. Remove any code that resolved the old settings singleton from the container.

### 4. `Newtonsoft.Json` removed from `Paramore.Darker`

`Paramore.Darker` no longer depends on `Newtonsoft.Json`, transitively or directly. Consumers who relied on it arriving transitively via Darker must add their own `Newtonsoft.Json` package reference.

---

## Migration

### Recommended migration (preserves the `ReferenceHandler.IgnoreCycles` default)

`AddJsonQueryLogging` mutates the existing `QueryLoggingJsonOptions.Options` instance, so the built-in `ReferenceHandler.IgnoreCycles` default (which keeps EF Core navigation-property cycles from throwing on the logging hot path) is preserved while you tweak other settings:

```csharp
services.AddDarker()
        .AddJsonQueryLogging(o => { o.WriteIndented = true; /* mutate as needed */ });
```

### Direct-assignment migration (⚠️ drops `IgnoreCycles` unless re-applied)

If you replace `QueryLoggingJsonOptions.Options` wholesale, you lose the `IgnoreCycles` default. **Re-apply it explicitly**, otherwise a query object with a reference cycle (e.g. an EF Core entity graph) will throw on the logging path:

```csharp
QueryLoggingJsonOptions.Options = new JsonSerializerOptions
{
    ReferenceHandler = ReferenceHandler.IgnoreCycles, // re-apply default
    WriteIndented = true,
};
```

### Swapping the serialiser entirely → write a custom decorator

There is **no** `IQueryLoggingSerializer` (or similar) interface to implement — this was considered and rejected (ADR 0012, Alternative 1: it would add a public type with no Brighter equivalent, a DI/constructor wiring path, per-call virtual dispatch on the hot path, and a versioning surface, for an unclear consumer benefit). To emit something other than `System.Text.Json` in the `{Query}` argument (MessagePack, Protobuf, Newtonsoft, …), write your own logging decorator that wraps your serialiser and **skip `AddJsonQueryLogging()`**. This is the supported escape hatch: ~10 lines for a sync-only quick case, ~50 for a production-quality sync+async pair (cached `static readonly` logger, fallback-bag handling, start/complete templates).

---

## Native AOT / trimming

### Logging path is AOT-clean

`JsonSerializer.Serialize` on the decorator hot path is annotated `[RequiresUnreferencedCode]` / `[RequiresDynamicCode]`, so it raises `IL2026` / `IL3050` under trim/AOT analysis. Darker suppresses these **locally** at the two call sites (`QueryLoggingDecorator.cs` and `QueryLoggingDecoratorAsync.cs`) via `[UnconditionalSuppressMessage]` with a justification (FR13 allow-list) — never a repo-wide `<NoWarn>`. These two sites are the **only** suppressed IL warnings under `src/Paramore.Darker/Logging/`. This was verified by a dedicated **native-AOT console harness** (`test/Paramore.Darker.Tests.AOT`, `PublishAot=true`, `TrimMode=full`) that publishes and runs to exit 0 on `net8.0` and `net9.0` with zero IL warnings under the logging path.

### Type-safe AOT opt-in via `TypeInfoResolver`

Consumers publishing with `PublishAot=true` who want a fully trim-safe, reflection-free path supply a source-generated `JsonSerializerContext` once at startup:

```csharp
QueryLoggingJsonOptions.Options.TypeInfoResolver = MyQueryJsonContext.Default;
```

Reflection-based serialization is still supported but is trim-unsafe (and disabled outright under full trimming, where an unrooted type throws `NotSupportedException`/`InvalidOperationException` at serialize time).

### ⚠️ Configure `QueryLoggingJsonOptions.Options` once, before the first query

`QueryLoggingJsonOptions.Options` is a **process-global** instance, and `System.Text.Json` **locks a `JsonSerializerOptions` instance on its first serialize**. Once any query has been logged, re-installing `TypeInfoResolver` or mutating the options throws `InvalidOperationException: "...instances cannot be modified..."`. Configure it exactly once, at application startup, before the first query is handled.

### ⚠️ AOT consumers must root the handler for the reflection pipeline

Darker's pipeline resolves a handler's `Execute` / `ExecuteAsync` via reflection (`Type.GetMethod`). Under `TrimMode=full` the trimmer removes those methods unless they are rooted, and the pipeline then throws `ConfigurationException` at runtime. A full-trim AOT consumer must root each handler:

```csharp
[DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(MyQueryHandler))]
```

This is **consumer-side compensation** for the pre-existing reflection pipeline (see the known limitation below), not a change introduced by the serialiser swap or to the logging path.

### Known AOT limitation — pre-existing pipeline/registry trim warnings (not introduced here, not suppressed)

Enabling AOT/trim analysis on Darker surfaces ~28 **pre-existing** `IL2xxx`/`IL3xxx` warnings in Darker's reflection-based pipeline, **outside** the logging path:

- `PipelineBuilder.cs` (`GetMethod`, `MakeGenericType` — lines 131, 147, 152, 214, 250)
- `QueryHandlerRegistry.cs` (`GetInterfaces`, `ExportedTypes`, `ImplementedInterfaces` — lines 44, 54, 57)
- `QueryHandlerRegistryAsync.cs` (same shapes — lines 43, 52, 55)

These are **not** introduced by this change, are **not** suppressed (deliberately — suppressing them would hide a real trim hazard), and remain a follow-up. A consumer AOT-publishing an app that resolves Darker handlers will see them. The build still succeeds (warnings only).

---

## Other limitations / notes

### Parallel `WebApplicationFactory` integration tests race on the static options

Because `QueryLoggingJsonOptions.Options` is process-global, consumer test suites that spin up multiple `WebApplicationFactory<TStartup>` hosts in parallel will race on it (and on its first-serialize lock). Serialise host construction:

```csharp
[CollectionDefinition("DarkerHostBootstrap", DisableParallelization = true)]
```

on the relevant test collection. (See constraint C6.)

### Builder-surface limitation — `IBuildTheQueryProcessor.JsonQueryLogging(...)` only supports the in-box builder

The fluent `IBuildTheQueryProcessor.JsonQueryLogging(...)` overload casts to the concrete in-box `QueryProcessorBuilder`; a **custom** `IBuildTheQueryProcessor` implementation throws `NotSupportedException`. Consumers using a custom builder should instead:

- use the DI extension `IDarkerHandlerBuilder.AddJsonQueryLogging(...)`, or
- call the canonical generic `AddJsonQueryLogging<TBuilder>(...)` directly.

### Not a consumer break: the internal xunit v2 → v3 test upgrade

Darker's own test infrastructure moved from xunit v2 to v3. This is **not** a consumer-facing breaking change: the shipped `Paramore.Darker.Testing` assembly has no xunit dependency, so the upgrade is invisible to consumers and is intentionally **not** listed as a break.
