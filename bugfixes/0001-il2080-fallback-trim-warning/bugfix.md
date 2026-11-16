# Bugfix: IL2080 trim warning on net11.0 at PipelineBuilder fallback lookups (unannotated handler Type through tuple)

**Linked Issue**: #409
**Status**: Verified

## Symptom

**Observed.** A Release build of `src/Paramore.Darker/Paramore.Darker.csproj` reports two `IL2080` trim-analysis warnings, and only for `TargetFramework=net11.0`:

- `PipelineBuilder.cs(101,38)`: `'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String, Type[])'. The field '(System.Type handlerType, Paramore.Darker.IQueryHandler handler).handlerType' does not have matching annotations.`
- `PipelineBuilder.cs(164,38)`: the same warning for the `FallbackAsync` lookup.

A whole-solution build on all four TFMs shows the other trim/AOT warnings (IL2026, IL2055, IL2070, IL2075, IL3050) identically on net8.0, net9.0, net10.0 and net11.0. IL2080 shows up only on net11.0, and only at these two lines. The build still succeeds, because these are warnings, not errors.

**Expected.** Trim analysis of the fallback lookups behaves the same on every TFM. Either the `Type` carries the annotation it needs, or the site is deliberately suppressed and documented. Either way, the fallback path must work in trimmed and Native AOT apps.

**Reproduction.**
```bash
git switch master   # at a39fa3d, the merge of #408
dotnet build src/Paramore.Darker/Paramore.Darker.csproj -c Release --no-incremental 2>&1 | grep IL2080
```

**Background.** ADR 0012 already records about 28 pre-existing trim/AOT warnings in the reflection pipeline (`PipelineBuilder`, `QueryHandlerRegistry*`) as a known AOT limitation (`docs/adr/0012-json-serializer-swap.md:107`). It prefers justified call-site suppression to a repo-wide `<NoWarn>` (`:61`, `:98`). So the new warning adds to a known, accepted set. It is not the first trim warning in this area.

## Suspected Location

Both fallback lookups (the warning sites):
- `src/Paramore.Darker/PipelineBuilder.cs:101`: `var fallbackMethodInfo = handlerType.GetMethod("Fallback", new[] { queryType });`
- `src/Paramore.Darker/PipelineBuilder.cs:102`: the `fallback` delegate calls `fallbackMethodInfo.Invoke(...)` with no null check.
- `src/Paramore.Darker/PipelineBuilder.cs:164`: `handlerType.GetMethod("FallbackAsync", new[] { queryType, typeof(CancellationToken) });`
- `src/Paramore.Darker/PipelineBuilder.cs:165`: the async `fallback` delegate, also with no null check.

Where `handlerType` comes from (a tuple field with no annotations):
- `src/Paramore.Darker/PipelineBuilder.cs:71`: `var (handlerType, handler) = ResolveHandler(...)`.
- `src/Paramore.Darker/PipelineBuilder.cs:130`: `var (handlerType, handler) = ResolveHandlerAsync(...)`.
- `src/Paramore.Darker/PipelineBuilder.cs:198`, `:212`: `ResolveHandler` returns `(Type handlerType, IQueryHandler handler)`.
- `src/Paramore.Darker/PipelineBuilder.cs:215`, `:233`, `:237`: `ResolveHandlerAsync` returns the same tuple, or falls through to `ResolveHandler`.
- `src/Paramore.Darker/IQueryHandlerRegistry.cs:15`, `src/Paramore.Darker/IQueryHandlerRegistryAsync.cs:15`: `Type Get(Type queryType, IQuery query, IQueryContext context);`, with no `[return: DynamicallyAccessedMembers]`.
- Implementations: `src/Paramore.Darker/QueryHandlerRegistry.cs:19`, `src/Paramore.Darker/QueryHandlerRegistryAsync.cs:19`. Registration entry points: `Register<TQuery, TResult, THandler>` at `QueryHandlerRegistry.cs:24` and `QueryHandlerRegistryAsync.cs:24`. `THandler` is not annotated there either.
- `src/Paramore.Darker` has no `DynamicallyAccessedMembers` anywhere. The only `UnconditionalSuppressMessage` uses are the IL2026/IL3050 suppressions in `Observability/DarkerTracer.cs:211-215`, `Logging/Handlers/QueryLoggingDecorator.cs:48-52`, `QueryLoggingDecoratorAsync.cs:52-56` and `StreamQueryLoggingDecorator.cs:95-99`.

The Execute path, for contrast (same `handlerType`, different route):
- `src/Paramore.Darker/PipelineBuilder.cs:76`, `:135`: `ResolveMethod(PipelineAttributeReader.FindExecute(handlerType))` / `FindExecuteAsync(handlerType)`.
- `src/Paramore.Darker/PipelineAttributeReader.cs:45-54`: `FindExecute(Type handlerType)`, `FindExecuteAsync(Type handlerType)` and `FindStreamExecuteAsync(Type handlerType, Type queryType)` take an unannotated `Type` parameter and call `handlerType.GetMethod(...)` inside a lambda passed to `Lookup`. This path does not avoid the problem. The analyzer just sees `handlerType` as a method parameter instead of a tuple field, and most likely reports it as IL2070 on every TFM.

Stream pipeline:
- `src/Paramore.Darker/PipelineBuilder.cs:276-325`: `BuildStream` has no fallback lookup and uses only `FindStreamExecuteAsync` (`:292`). `ResolveStreamHandler` (`:372`, `:393`) returns the same tuple shape, but `PipelineBuilder` calls no `GetMethod` on its `handlerType`. We expect no stream counterpart at a fallback site.

Who calls the fallback at run time:
- `src/Paramore.Darker/Policies/Handlers/FallbackPolicyDecorator.cs:38`: `return fallback(query);`
- `src/Paramore.Darker/Policies/Handlers/FallbackPolicyDecoratorAsync.cs:43`: `return await fallback(query, cancellationToken)...`

Handler contracts (relevant to H-fix-3):
- `src/Paramore.Darker/IQueryHandler.cs:13`: `TResult Fallback(TQuery query);` on `IQueryHandler<in TQuery, TResult>`.
- `src/Paramore.Darker/IQueryHandlerAsync.cs:11`: `Task<TResult> FallbackAsync(TQuery query, CancellationToken cancellationToken = default)` on `IQueryHandlerAsync<in TQuery, TResult>`.
- Base implementations: `src/Paramore.Darker/QueryHandler.cs:15`, `src/Paramore.Darker/QueryHandlerAsync.cs:17` (both `public virtual`).
- `PipelineBuilder<TResult>` is generic only over `TResult`, and it holds the handler as a non-generic `IQueryHandler` (`PipelineBuilder.cs:16`, `:30`). It cannot call the interface directly without knowing the concrete query type.

Build and AOT configuration:
- `src/Paramore.Darker/Paramore.Darker.csproj`: `TargetFrameworks` is `netstandard2.0;net8.0;net9.0;net10.0;net11.0`, with `<IsAotCompatible Condition="'$(TargetFramework)' != 'netstandard2.0'">true</IsAotCompatible>`. The comment above it still says "net8/net9 only" (stale). No `IsTrimmable` or `NoWarn` is set.
- `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`: `net8.0;net9.0;net10.0;net11.0`, `PublishAot=true`, `TrimMode=full`.
- `test/Paramore.Darker.Tests.AOT/Program.cs:13-17` runs only `Case1PropertyBearingJson` and `Case2CycleBearingNoThrow`. No `.cs` file in the project mentions `Fallback` or `Policy`.
- `test/Paramore.Darker.Tests.AOT/Scenarios/Case1PropertyBearingJson.cs:32` and `Case2CycleBearingNoThrow.cs:29` put `[DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(<Handler>))]` on the scenario. `Case2CycleBearingNoThrow.cs:26-28` calls this "consumer-side compensation" for "PipelineBuilder's documented AOT known-limitation". It keeps all of the handler's public methods, including the inherited `FallbackAsync`.

Related ADRs:
- `docs/adr/0012-json-serializer-swap.md:61`, `:98`, `:107`: suppress at known-safe sites with a justification, never with a repo-wide `<NoWarn>`. The pre-existing PipelineBuilder and registry warnings are a known limitation.
- `docs/adr/0027-end-to-end-verification-outside-developer-tests.md:46`, `:169`: `Paramore.Darker.Tests.AOT` "stays as it is" within that ADR's scope. That limits which ADR covers new AOT scenarios, not whether we may add them.

A nearby issue, outside this bug: the `fallback` delegates at `PipelineBuilder.cs:102` and `:165` do not unwrap `TargetInvocationException`, but the sink delegates at `:92-96` and `:155-159` do. It is recorded here only so /bugfix:confirm can decide whether it is in scope.

## Root-Cause Hypothesis

**Primary hypothesis (H1).** The code has always had a trim gap: nothing guarantees that the handler `Type` keeps its public methods. `PipelineBuilder.cs:101` and `:164` call `Type.GetMethod(string, Type[])`, which requires `[DynamicallyAccessedMembers(PublicMethods)]` on its `this`. The `Type` they use is the `handlerType` field of a `(Type, IQueryHandler)` tuple, built from the unannotated `IQueryHandlerRegistry(Async).Get`. The trim analyzer that ships with the net11.0 targeting pack (the per-TFM `Microsoft.NET.ILLink.Tasks`) sees the deconstructed tuple value as coming from an unannotated field, and reports IL2080. The net8.0, net9.0 and net10.0 analyzers report **no** diagnostic at lines 101 and 164: no IL2080, IL2072, IL2075 or anything else. We guess they treat the value as unknown and stay silent, but this mechanism is part of the hypothesis. The warning therefore comes from the newer analyzer, not from a code change in #408. **UNVERIFIED — to be proven or refuted in /bugfix:confirm.**

H1 is falsified if:
- (a) net8, net9 or net10 report any trim diagnostic at `PipelineBuilder.cs:101` or `:164`. The warning would then exist on every TFM under a different code, and the "only net11" framing is wrong.
- (b) The net11 diagnostic goes away without any change to how `handlerType` flows. That points to a different cause, for example a changed annotation on that `GetMethod` overload in the net11 reference assembly.
- (c) Returning `Type` directly instead of through a tuple (as a local experiment) still gives IL2080 or IL2075 on net11 at those lines. The tuple would then be incidental and the root the unannotated `Get` return. This does not falsify "the gap was already there", but it narrows the mechanism.

**The issue's own causal claims, restated as hypotheses:**
- **H2 (from issue claim (a)).** The net8–net10 analyzers "do not track annotations through tuple deconstruction". Other explanations are just as plausible and untested: the newer analyzer models unknown values differently, or net11 changed the annotations on `Type.GetMethod(string, Type[])`. **UNVERIFIED — to be proven or refuted in /bugfix:confirm.**
- **H3 (from issue claim (b)).** In a trimmed or AOT app where the trimmer removes the handler's public `Fallback`/`FallbackAsync`, `GetMethod` returns `null`. `FallbackPolicyDecorator(Async)` then throws a `NullReferenceException` from `fallbackMethodInfo.Invoke` (`PipelineBuilder.cs:102` / `:165`) instead of running the fallback. The mechanics are right: there is no null guard. Whether the trimmer actually removes those methods is open. They implement interface methods (`IQueryHandler.cs:13`, `IQueryHandlerAsync.cs:11`) on a type the app constructs. ILLink keeps an implementation when the interface method is kept, but the interface method may have no static caller. An NRE thrown inside a lambda may also come wrapped differently from what the issue assumes. **UNVERIFIED — to be proven or refuted in /bugfix:confirm.**
- **H4 (from issue claim (c)).** "`Paramore.Darker.Tests.AOT` does not currently exercise fallback." The static evidence supports this, since no fallback or policy scenario exists. However, the scenarios use `[DynamicDependency(PublicMethods, typeof(Handler))]` (`Case1PropertyBearingJson.cs:32`, `Case2CycleBearingNoThrow.cs:29`), which keeps `FallbackAsync`. A new scenario written in the same style would therefore **not** detect H3. **UNVERIFIED — to be proven or refuted in /bugfix:confirm.**

**The issue's "Possible fixes", treated as untested options rather than decisions:**
- **H-fix-1.** Add `[return: DynamicallyAccessedMembers(PublicMethods)]` to the registry `Get` methods (`IQueryHandlerRegistry.cs:15`, `IQueryHandlerRegistryAsync.cs:15`) and let it flow, either by annotating the helpers or by returning `Type` directly. This changes public interfaces. The annotation may also need to cascade to `Register(Type, Type, Type)` and to the router-based `Register` overloads (`IQueryHandlerRegistry.cs:37`), where a user router supplies the handler type. It also runs into the netstandard2.0 condition in the csproj. **UNVERIFIED — to be proven or refuted in /bugfix:confirm.**
- **H-fix-2.** Annotate `THandler` on `Register<TQuery, TResult, THandler>` and add a justified `UnconditionalSuppressMessage("Trimming", "IL2080", ...)` at the two call sites, following ADR 0012's precedent (`docs/adr/0012-json-serializer-swap.md:98`). It is open whether this covers the router and `Type`-based registration paths. **UNVERIFIED — to be proven or refuted in /bugfix:confirm.**
- **H-fix-3.** Call `Fallback`/`FallbackAsync` through the handler interface instead of `GetMethod`. The interfaces declare these methods (`IQueryHandler.cs:13`, `IQueryHandlerAsync.cs:11`). But `PipelineBuilder<TResult>` does not know `TQuery`, and it holds the handler as a non-generic `IQueryHandler` (`PipelineBuilder.cs:16`, `:30`). It is open whether this can work without new reflection or `MakeGenericType`, which would bring IL2055/IL3050. **UNVERIFIED — to be proven or refuted in /bugfix:confirm.**

**What /bugfix:confirm should run or check:**
1. **Reproduce per TFM and compare diagnostics by line** at `PipelineBuilder.cs` 76, 101, 135, 164 and 292, and `PipelineAttributeReader.cs` 46, 50 and 54:
   ```bash
   for tfm in net8.0 net9.0 net10.0 net11.0; do
     echo "== $tfm"; dotnet build src/Paramore.Darker/Paramore.Darker.csproj -c Release -f $tfm --no-incremental 2>&1 \
       | grep -E "warning IL(2|3)[0-9]{3}" | grep -E "PipelineBuilder|PipelineAttributeReader" | sort -u
   done
   ```
   This tests H1(a) and H2, and shows which code (IL2070?) the Execute path gets on each TFM, as a contrast.
2. **Find out which analyzer runs for each TFM.** Check `obj/project.assets.json` for the `Microsoft.NET.ILLink.Tasks` version per TFM, and which SDK is in use (`dotnet --version`, `global.json`). This tests whether analyzer versions explain the difference.
3. **Compare the `GetMethod(string, Type[])` annotation** in the net10 and net11 reference assemblies, to rule out the overload explanation in H1(b) and H2.
4. **Throwaway local experiments, not fixes.** Replace the tuple with a direct `Type` return, or add `[return: DynamicallyAccessedMembers(PublicMethods)]` to `ResolveHandler`. Re-run step 1 on net11 and net10, then revert. This tests H1(c) and H2.
5. **Test H3 at run time.** Write a scratch AOT scenario, outside the repo, that registers a handler deriving from `QueryHandler<,>`/`QueryHandlerAsync<,>`. It should override `Fallback`/`FallbackAsync`, use `[FallbackPolicy]` and have a throwing `Execute`, and it must **not** have `[DynamicDependency(PublicMethods, …)]`. Publish with `dotnet publish test/Paramore.Darker.Tests.AOT -c Release -f net11.0 -r osx-arm64`, run the native binary, and see whether the fallback runs, `GetMethod` returns null, or Execute discovery fails first. Execute uses the same unannotated `Type`, so without `DynamicDependency` the Execute lookup (`PipelineAttributeReader.cs:46` / `:50`) may fail before fallback is reached. That would make H3 real but not the first failure. Repeat with `DynamicDependency` to confirm H4: it hides the bug.
6. **Confirm H4 statically.** Grep `test/Paramore.Darker.Tests.AOT/**/*.cs` for `Fallback`/`FallbackPolicy`, and read `Program.cs`.
7. **Check how far H-fix-1 or H-fix-2 would reach.** List every route by which a handler `Type` enters the registry: `Register<,,>`, `Register(Type, Type, Type)`, router `Register<TQuery, TResult>(router, params Type[])`, and the DI extension registries in `src/Paramore.Darker.Extensions.DependencyInjection`. Note which routes could carry an annotation, and whether netstandard2.0 compiles the attributes.

## Confirmed Root Cause

**Verdict: CONFIRMED, with a refined mechanism.** H1–H4 all hold.

`PipelineBuilder<TResult>` finds the handler's fallback by name, with a public-only reflective lookup: `handlerType.GetMethod("Fallback", …)` at `src/Paramore.Darker/PipelineBuilder.cs:101` and `GetMethod("FallbackAsync", …)` at `:164`. It then calls `fallbackMethodInfo.Invoke(...)` in the `fallback` delegate (`:102`, `:165`) with no null check. Nothing guarantees that this lookup succeeds:

- **The handler `Type` never carries a `PublicMethods` guarantee.** It comes from `IQueryHandlerRegistry(Async).Get` (`IQueryHandlerRegistry.cs:15`, `IQueryHandlerRegistryAsync.cs:15`). That in turn comes from `IResolveHandlers.ResolveHandlerType` (`IResolveHandlers.cs:18`), which is either `FixedHandlerRoute._handlerType` (`FixedHandlerRoute.cs:12,20`) or a user router (`RoutedHandlers.cs:45`). None of these is annotated with `[DynamicallyAccessedMembers]`. `src/Paramore.Darker` has no `DynamicallyAccessedMembers` or `DynamicDependency` at all, and no polyfill for netstandard2.0.
- **Nothing statically calls `Fallback`/`FallbackAsync`.** They are declared on `IQueryHandler.cs:13` and `IQueryHandlerAsync.cs:11`, and implemented virtually at `QueryHandler.cs:15` and `QueryHandlerAsync.cs:17`. With nothing rooting them, the trimmer is free to remove them.

So `GetMethod` returns `null` in two situations, and `FallbackPolicyDecorator(Async)` then gets a `NullReferenceException` (not wrapped) instead of the fallback's result:
1. **Trimmed / NativeAOT apps** where `Fallback` is not otherwise rooted.
2. **Under plain JIT**, for a handler that implements `Fallback`/`FallbackAsync` **explicitly**, because the public-only `GetMethod` cannot see it.

**Why the warning appears only on net11.0.** The root is the unannotated `Get` return. Used directly, it warns IL2075 on *every* TFM. `ResolveHandler` / `ResolveHandlerAsync` pass it through a `(Type handlerType, IQueryHandler handler)` `ValueTuple` (`PipelineBuilder.cs:198,212,215,233,237`, deconstructed at `:71` and `:130`). The net8–10 trim analyzers lose dataflow through tuple construction and deconstruction and report nothing. The net11 analyzer (ILLink 11.0.0) follows the value and reports IL2080 against the unannotated tuple field. This is a gap that was always there, now visible. #408 did not cause it.

## Evidence

- [x] **Red repro: NativeAOT** (scratch probe outside the repo; net11.0, `PublishAot=true`, `TrimMode=full`, osx-arm64). It uses a sync `QueryHandler<,>` with `[FallbackPolicy(1)]`, an `Execute` that throws and an overridden `Fallback`, plus an async twin with `[FallbackPolicyAttributeAsync(1)]`:

  | Rooting | Sync | Async |
  |---|---|---|
  | JIT baseline | `FALLBACK-RAN` ✅ | `FALLBACK-ASYNC-RAN` ✅ |
  | V0 `[DynamicDependency(PublicMethods, typeof(H))]` (the existing AOT-test style) | ✅ | ✅ |
  | V1 `[DynamicDependency("Execute"/"ExecuteAsync", typeof(H))]` only | ❌ `NullReferenceException` in `<Build>b__0`, called from `FallbackPolicyDecorator.Execute` | ❌ `NullReferenceException` in `<BuildAsync>b__0`, called from `FallbackPolicyDecoratorAsync.ExecuteAsync` |
  | V2 no rooting | ❌ NRE in `PipelineAttributeReader.CheckAttributeKinds` (the Execute lookup fails first, see Scope Note 3) | `ConfigurationException` "does not implement ExecuteAsync" |

- [x] **Red repro: plain JIT, no trimming.** The same probe with the sync handler implementing `IQueryHandler<FbQuery,string>` directly and `Fallback` implemented **explicitly**. The fallback fails with `NullReferenceException at PipelineBuilder`1.<>c__DisplayClass17_0.<Build>b__0 … PipelineBuilder.cs:line 102`, called from `FallbackPolicyDecorator.cs:line 38`. The line numbers show that the failing lambda is the fallback delegate (`:102`), not the Execute sink. **This repro runs in the ordinary xunit suite (Core.Tests); no AOT publish is needed.**

- [x] **Analyzer evidence** (`dotnet build src/Paramore.Darker -c Release -f <tfm> --no-incremental`, SDK 11.0.100):
  - net8.0, net9.0 and net10.0 give the same trim-warning set, with nothing at `PipelineBuilder.cs:101` or `:164`. net11.0 gives the same set plus IL2080 at `(101,38)` and `(164,38)`.
  - Analyzer per TFM: `Microsoft.NET.ILLink.Tasks` 8.0.34, 9.0.23, 10.0.15 and 11.0.0.
  - `PipelineAttributeReader.cs:54` calls the same `GetMethod(string, Type[])` overload and warns IL2070 on net8.0. The overload's annotation did not change in net11, which refutes H1(b) and that half of H2.
  - **Experiment E1** (scratch copy). `_handlerRegistry.Get(...).GetMethod("Fallback", …)` used directly gives IL2075 on net8, net10 and net11. The same value through a local tuple is silent on net8/net10 and gives IL2075 on net11. This confirms H2.
  - **Experiment E2** (scratch copy). Adding `[return: DynamicallyAccessedMembers(PublicMethods)]` to `IQueryHandlerRegistry.Get` and `QueryHandlerRegistry.Get` while keeping the tuple still gives IL2080 at `:101` and `:164` on net11. It also adds IL2073 at `QueryHandlerRegistry.cs:21` on every TFM, because `IResolveHandlers` is not annotated.

- [x] **Code-trace.**
  - `FallbackPolicyDecorator.cs:30` calls `next` inside a `try`. When `_exceptionTypes` is empty, `:32-38` catches and calls `fallback(query)` at `:38`. The async version: `FallbackPolicyDecoratorAsync.cs:35` / `:39-43`.
  - `_handler` is non-null (`PipelineBuilder.cs:73`, `:132`, guarded at `:209`), so the only null that can be dereferenced in the fallback delegate is `fallbackMethodInfo`.
  - The AOT tests never exercise fallback: `test/Paramore.Darker.Tests.AOT/Program.cs:13-17` runs only Case1 and Case2. Their `[DynamicDependency(PublicMethods, …)]` (`Case1PropertyBearingJson.cs:32`, `Case2CycleBearingNoThrow.cs:29`) would hide the bug in any fallback scenario written the same way (H4).

**Suggested-fix assessment** (from the issue and the triage):
- **H-fix-1 (annotate `Get` and let it flow): PARTIAL.** It addresses the right cause, but E2 proves that annotating `Get` alone removes neither IL2080: a `ValueTuple` element cannot carry the annotation, so the tuple must go. Done fully, it would also have to touch:
  - the public `IResolveHandlers` interface;
  - `FixedHandlerRoute` and `RoutedHandlers`, where a router `Func<…, Type?>` and a `params Type[]` of candidates cannot be annotated, so a suppression stays;
  - `Register(Type, Type, Type)` and `RegisterFromAssemblies`;
  - the DI overrides `ServiceCollectionHandlerRegistry(Async)` and `ServiceCollectionStreamHandlerRegistry`, and any overrides in user code, to avoid IL2092–2095;
  - netstandard2.0, which needs `#if` or a polyfill.

  It is the most "correct" option, but it is large and changes public API.
- **H-fix-2 (annotate `THandler` on `Register<,,>` and suppress IL2080): PARTIAL, and risky.** It roots `PublicMethods` only on the generic registration path. Router, `Type`-based, assembly-scanned and DI `AddHandlersFromAssemblies` registrations keep the NRE, and the suppression would claim they are safe when they are not. It is acceptable only together with a null guard that throws a clear `ConfigurationException`, and an honest justification.
- **H-fix-3 (call through the interface): PARTIAL, and more feasible than the triage suggested.** It can't be done inside `PipelineBuilder` alone, because it knows only `TResult` and holds a non-generic `IQueryHandler`. It *is* feasible without reflection for handlers derived from `QueryHandler<,>` / `QueryHandlerAsync<,>`, which know `TQuery` and could implement a `TResult`-only contract. A typed invoker could also be captured at generic `Register<TQuery,TResult,THandler>` time. Direct interface implementers (e.g. `test/Paramore.Darker.Extensions.Tests/When_sync_routing_registration_used_should_register_all_candidate_handlers.cs:14,21`), `Register(Type, Type, Type)` and scanning would still need the reflective path, with a guard. Calling through the interface would also fix the explicit-implementation case under JIT, and remove the `TargetInvocationException` wrapping (Scope Note 5).

## Scope Notes

1. **Sync and async fallback sites: IN scope.** `PipelineBuilder.cs:101-102` and `:164-165` have the same defect, and the fix must treat both the same way.
2. **Stream pipeline: no counterpart.** `BuildStream` (`PipelineBuilder.cs:276-325`) has no fallback lookup, and `ResolveStreamHandler` (`:372-394`) feeds only `FindStreamExecuteAsync`, which already warns IL2070 on every TFM. Out of scope, unless the chosen fix is H-fix-1, in which case the stream registries need the same annotations for consistency.
3. **Sync Execute path has no null guard: SEPARATE issue.** There is no check between `PipelineBuilder.cs:76` and `:77`. The async path has one at `:136-137`, and the stream path at `:293-294`. If `Execute` is trimmed (V2), or implemented explicitly, sync throws a rethrown NRE from `PipelineAttributeReader.CheckAttributeKinds` (`:99`, via `AttributeReadFailure.Rethrow`) instead of a `ConfigurationException`. It is a real sync/async parity gap, but in the Execute lookup, not at the IL2080 fallback site. Raise it separately, and keep any new fallback guard's message consistent with it.
4. **Explicit-interface `Fallback`/`FallbackAsync`: IN scope.** Proven above to throw an NRE under plain JIT. It has the same root (a public-only lookup by name with no guard), and it gives the cheapest red regression test.
5. **No `TargetInvocationException` unwrapping in the fallback delegates: SEPARATE issue.** `:102` and `:165` don't unwrap, but the sinks do (`:92-96`, `:155-159`). So a user `Fallback` that throws surfaces as a `TargetInvocationException`. This changes the exception callers see, and changing it is a behaviour change. Raise it separately, unless H-fix-3 is chosen: a direct interface call removes the wrapping, and the PR must then call that out.
6. **AOT test coverage: IN scope for the regression test.** Any new fallback scenario in `test/Paramore.Darker.Tests.AOT` must root only `Execute`/`ExecuteAsync` by name (the V1 shape). Rooting with `PublicMethods` passes even with the bug present.
7. **Stale csproj comment: IN scope only if the fix edits the csproj anyway.** `Paramore.Darker.csproj:5-6` still says "net8/net9 only".

## Regression Test

**Expected behaviour (decided at the test gate):** the fallback **runs**. An explicit implementation of `IQueryHandler<,>.Fallback` / `IQueryHandlerAsync<,>.FallbackAsync` meets the interface contract, so the pipeline must call it. A clear `ConfigurationException` was the alternative, and it was rejected. So a null guard alone does not satisfy these tests. The fix must reach `Fallback` through the interface, not by public name.

All three tests are approved and RED today:

1. **Sync, JIT:** `test/Paramore.Darker.Core.Tests/When_a_sync_handler_implements_fallback_explicitly_should_return_the_fallback_result.cs` (class `ExplicitFallbackSyncTests`), using double `TestDoubles/ExplicitFallbackSyncHandler.cs`. It has a public `Execute` that throws under `[FallbackPolicy(1)]` and an explicit `Fallback` that returns the query id. It asserts `result.Value == queryId`.
   - RED: `NullReferenceException` at `PipelineBuilder.cs:102` ← `FallbackPolicyDecorator.cs:38`.
2. **Async, JIT:** `test/Paramore.Darker.Core.Tests/When_an_async_handler_implements_fallback_explicitly_should_return_the_fallback_result.cs` (class `ExplicitFallbackAsyncTests`), using double `TestDoubles/ExplicitFallbackAsyncHandler.cs`. It has a public `ExecuteAsync` that throws under `[FallbackPolicyAttributeAsync(1)]` and an explicit `FallbackAsync`.
   - RED: `NullReferenceException` at `PipelineBuilder.cs:165` ← `FallbackPolicyDecoratorAsync.cs:43`.
3. **NativeAOT, sync and async:** `test/Paramore.Darker.Tests.AOT/Scenarios/Case3FallbackRunsWhenOnlyExecuteIsRooted.cs`, with handlers in `Scenarios/AotFallbackQuery.cs`, wired into `Program.cs` after Case2. It roots only `Execute`/`ExecuteAsync` by name (the V1 shape, see Scope Note 6), and expects both fallbacks to return the query id.
   - RED under `dotnet publish test/Paramore.Darker.Tests.AOT -c Release -f net11.0 -r osx-arm64`: `[Case3] FAIL … NullReferenceException` at `PipelineBuilder<Build>b__0` ← `FallbackPolicyDecorator.Execute`, exit 1.
   - Passes under JIT (`dotnet run -f net11.0`).
   - The sync call fails first, so this run never reaches the async call; the confirm probe showed it fails the same way.
   - **Note:** no CI workflow runs the AOT harness. It is verified by hand.

## Fix

**Summary:** handlers derived from `QueryHandler<,>` or `QueryHandlerAsync<,>` now have their fallback called directly, with no reflection. Any other handler's fallback is found through its closed `IQueryHandler<,>` / `IQueryHandlerAsync<,>` interface, which also reaches explicit implementations. This replaces the public-only `GetMethod` by name at both fallback sites. The design was chosen at the fix gate as "base-class call + interface lookup". It is a straight fix; no tidy-first refactor was needed.

**Files changed:**
- `src/Paramore.Darker/IAmAFallbackTarget.cs` (new, **internal**): `TResult Fallback(IQuery<TResult> query)`.
- `src/Paramore.Darker/IAmAFallbackTargetAsync.cs` (new, **internal**): `Task<TResult> FallbackAsync(IQuery<TResult> query, CancellationToken cancellationToken)`.
- `src/Paramore.Darker/QueryHandler.cs`: explicitly implements `IAmAFallbackTarget<TResult>` by casting to `TQuery` and calling `Fallback`.
- `src/Paramore.Darker/QueryHandlerAsync.cs`: explicitly implements `IAmAFallbackTargetAsync<TResult>` the same way.
- `src/Paramore.Darker/PipelineBuilder.cs`:
  - The sync and async fallback sites (formerly `:101-102` and `:164-165`) now call `ResolveFallback` / `ResolveFallbackAsync`.
  - Each of these uses the internal role when `_handler` implements it. Otherwise it calls `FindHandlerInterfaceMethod`, which picks the closed `IQueryHandler(Async)<queryType, TResult>` interface and invokes that interface's `Fallback`/`FallbackAsync` `MethodInfo`.

**Public API:** unchanged. The new interfaces are internal, and the base classes implement them explicitly.

**Results:**
- `ExplicitFallbackSyncTests` and `ExplicitFallbackAsyncTests`: GREEN on net8.0, net9.0, net10.0 and net11.0.
- AOT harness, `dotnet publish … -f net11.0 -r osx-arm64`: Case1, Case2 and **Case3 PASS**, exit 0.
- netstandard2.0 still builds.

**Trim warnings** (compared with the pre-fix builds, per TFM):

| TFM | Removed | Added |
|---|---|---|
| net11.0 | IL2080 at `PipelineBuilder.cs:101` and `:164` | IL2070 `(205,16)`, IL2075 `(210,18)` |
| net8.0, net9.0, net10.0 | — | IL2070 `(205,16)`, IL2075 `(210,18)` |

The added warnings are `FindHandlerInterfaceMethod`'s `GetInterfaces()` / `GetMethod`. That branch is now used only for handlers that implement the interfaces directly, and it is reported **the same on every TFM**, like the Execute lookups in `PipelineAttributeReader`. It is still not safe for such handlers under Native AOT. It is deliberately not suppressed, following ADR 0012. **Tracked in #410.** A Native AOT probe against this fix confirmed it: a direct implementer rooted only on `Execute` still throws a `NullReferenceException` from `<ResolveFallback>b__0`, while a `QueryHandlerAsync<,>` subclass's fallback runs.

**Behaviour notes for the PR:**
- **Calling base-class handlers directly** changes one exception detail. An exception thrown from a `QueryHandler<,>` fallback was previously wrapped in a `TargetInvocationException` inside the pipeline. It now reaches decorators *outside* the fallback decorator unwrapped. At the `QueryProcessor` boundary nothing changes, because `QueryProcessor.cs:83` and `:124` already unwrap `TargetInvocationException`; `PipelineBuilderExceptionTests` relies on this. This also corrects Scope Note 5: callers never saw the wrapper.
- **Direct interface implementers** (the reflective branch) are unchanged on that point: inside the pipeline, their fallback exceptions are still wrapped.

**Not changed (separate issues, per Scope Notes 3 and 5):** the sync Execute null guard (`PipelineBuilder.cs:76-77`), and unwrapping in the reflective fallback delegate.
