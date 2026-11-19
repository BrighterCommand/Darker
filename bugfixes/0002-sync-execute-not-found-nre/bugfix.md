# Bugfix: Sync pipeline throws NullReferenceException instead of ConfigurationException when Execute cannot be found

**Linked Issue**: #412
**Status**: Verified

## Symptom
When `PipelineAttributeReader.FindExecute` returns no method for a sync handler, `QueryProcessor.Execute` throws a `NullReferenceException`. Its stack trace points to `PipelineAttributeReader.CheckAttributeKinds`. The async and stream pipelines throw a `ConfigurationException` that names the handler in the same situation.

- **Observed (sync):** `NullReferenceException`. The original exception type and stack trace are kept, so the caller sees a raw NRE, not a wrapper.
- **Observed (async, for comparison):** `ConfigurationException`: "Handler {handlerType.FullName} does not implement ExecuteAsync. Register an async handler or use Execute instead." The message uses `FullName`, so it carries the namespace. The issue quotes the short name `ExplicitExecuteAsyncHandler`.
- **Expected:** sync throws a `ConfigurationException` naming the handler, as async and stream do.

**When it happens**
- Under plain JIT, when a handler implements `IQueryHandler<,>.Execute` explicitly, so it has no public `Execute`.
- In trimmed or Native AOT apps, when `Execute` is not rooted. Background: bugfix 0001, Evidence row "V2 no rooting" and Scope Note 3.

**Repro (from the issue)**
1. Write a handler implementing `IQueryHandler<Q, string>` with an explicit `Execute`.
2. Register it with `QueryHandlerRegistry.Register<,,>()` and `SimpleHandlerFactory`.
3. Call `QueryProcessor.Execute(...)`.
4. Expected `ConfigurationException`; got `NullReferenceException`.

## Suspected Location
**`FindExecute` only sees public methods**
- `src/Paramore.Darker/PipelineAttributeReader.cs:45-46`: `FindExecute` is `Lookup(() => handlerType.GetMethod(EXECUTE_METHOD_NAME))`.
- `GetMethod(string)` with default binding flags finds public members only. An explicit implementation is private (named `IQueryHandler<Q,R>.Execute`), so it returns null.
- `FindExecuteAsync` (`:49-50`) and `FindStreamExecuteAsync` (`:53-54`) work the same way.

**A missing method is not a failure**
- `src/Paramore.Darker/PipelineAttributeReader.cs:137-147`: `Lookup` turns an exception into an `AttributeReadFailure`.
- When `GetMethod` simply returns null, `Lookup` returns `ExecuteMethodLookup(null, null)`: no method, and no failure.

**`ResolveMethod` passes null through**
- `src/Paramore.Darker/PipelineBuilder.cs:213-217`: `ResolveMethod` calls `lookup.ThrowIfFailed()`, which does nothing when there is no failure, then returns `lookup.Method`.

**Sync `Build` has no null guard**
- `src/Paramore.Darker/PipelineBuilder.cs:76-77`: `Build` assigns `executeMethodInfo` and passes it straight to `ValidateNoMismatchedAttributes(executeMethodInfo, HandlerPath.Sync)`.

**Async and stream have guards**
- `src/Paramore.Darker/PipelineBuilder.cs:134-136`: async throws `ConfigurationException` when the method is null.
- `src/Paramore.Darker/PipelineBuilder.cs:321-323`: stream does the same.

**Where the null is dereferenced**
- `src/Paramore.Darker/PipelineBuilder.cs:219-225`: `ValidateNoMismatchedAttributes` calls `CheckAttributeKinds`, then `check.ThrowIfFailed()` at `:222`.
- `src/Paramore.Darker/PipelineAttributeReader.cs:93-110`: `CheckAttributeKinds` takes `MemberInfo? method` and dereferences `method!.GetCustomAttributes(...)` at `:99`. The `!` hides the nullable warning.
- The catch at `:106-108` packs the NRE into an `AttributeReadFailure`.

**How the NRE is rethrown**
- `src/Paramore.Darker/ReadOutcome.cs`: `ThrowIfFailed` calls `Failure?.Rethrow()`.
- `src/Paramore.Darker/AttributeReadFailure.cs`: `Rethrow()` uses `ExceptionDispatchInfo.Capture(Exception).Throw()`.
- `AttributeReadFailure` only carries the exception; it is not an exception type itself. So the caller gets the original `NullReferenceException`, with its stack trace still pointing at `CheckAttributeKinds`.

**`QueryProcessor` does not wrap or translate it**
- `src/Paramore.Darker/QueryProcessor.cs:77`: `pipelineBuilder.Build(...)` runs before the `try` at `:79-98`.
- So the `TargetInvocationException` unwrap (`:83-88`), the log and span recording (`:89-94`) and `EndSpan` do not apply. The NRE propagates unchanged out of the `using` block (`:67`), which only disposes the builder.

**For comparison**
- `src/Paramore.Darker/PipelineValidation/PipelineDescriber.cs:76-77`: the pipeline validator checks for a null method before calling `CheckAttributeKinds`, so the builder is the only caller without a guard.

**Could `QueryHandler<,>` users hit this?**
- `src/Paramore.Darker/QueryHandler.cs:13` declares `public abstract TResult Execute(TQuery query)`, so every subclass has a public override and `GetMethod("Execute")` finds it under JIT.
- They may still hit it in trimmed or AOT apps if the override is not rooted. Bugfix 0001 Scope Note 3 points that way, but it is not proven for base-class handlers specifically.
- A subclass that adds a second public `Execute` overload gets an `AmbiguousMatchException` instead. That is a different failure, already caught by `Lookup` and not part of this issue.

**Test coverage of the async and stream null guards: none found**
- No test under `test/` asserts the message at `PipelineBuilder.cs:136` or `:323`.
- `test/Paramore.Darker.Core.Tests/TestDoubles/ExplicitInterfaceAsyncHandler.cs` (an explicit `ExecuteAsync` handler) is used only by the validator test `test/Paramore.Darker.Core.Tests/PipelineValidation/When_a_handler_implements_the_interface_explicitly_should_report_handler_matches_path_with_advice.cs:20`, not by any `QueryProcessor` test.
- The other async and stream `ConfigurationException` tests cover a different case: no handler registered at all (e.g. `When_stream_query_passed_to_ExecuteAsync_should_throw_ConfigurationException.cs`).

## Root-Cause Hypothesis
Sync `PipelineBuilder.Build` has no null check on the method `FindExecute` returns, unlike `BuildAsync` and `BuildStream`.

`FindExecute` uses public-only `GetMethod("Execute")`, so it returns `ExecuteMethodLookup(null, null)` when `Execute` is explicit or trimmed. `ResolveMethod` hands back that null without throwing. `CheckAttributeKinds` then dereferences it at `PipelineAttributeReader.cs:99`, catches the NRE as an `AttributeReadFailure`, and `ValidateNoMismatchedAttributes` rethrows the original NRE through `ExceptionDispatchInfo`. `QueryProcessor.Execute` calls `Build` outside its `try`, so the raw NRE reaches the caller.

**How to falsify it**
- **Prediction 1:** a sync handler with only an explicit `IQueryHandler<,>.Execute`, run through `QueryProcessor.Execute` under JIT, throws exactly `NullReferenceException`, not a `ConfigurationException` and not a `TargetInvocationException`.
- **Prediction 2:** the innermost frame of that exception is `PipelineAttributeReader.CheckAttributeKinds`.
- **Prediction 3:** a null guard placed between `PipelineBuilder.cs:76` and `:77` turns it into a `ConfigurationException`.
- If the NRE comes from somewhere else, or `FindExecute` actually finds the explicit method, the hypothesis is wrong.

**Issue suggestions, all UNVERIFIED — to be proven or refuted in /bugfix:confirm**
- Add a sync null check that throws a `ConfigurationException` naming the handler, matching async (`:135-136`) and stream (`:322-323`). **UNVERIFIED.**
- Reword all three messages, because "does not implement ExecuteAsync" is misleading when the method exists but is explicit or trimmed. **UNVERIFIED.**
- Supporting an explicit `Execute` by finding it through the handler interface, as #411 did for `Fallback` (`PipelineBuilder.cs:204-210`, `FindHandlerInterfaceMethod`), is a separate design question and out of scope here. **UNVERIFIED.**

## Confirmed Root Cause
Sync `PipelineBuilder.Build` has no null guard on the method `FindExecute` returns. `BuildAsync` and `BuildStream` both have one, so only the sync path is affected.

1. `FindExecute` uses public-only `GetMethod("Execute")`. For an explicit or trimmed `Execute`, it returns `ExecuteMethodLookup(null, null)`, with no failure recorded.
2. `ResolveMethod` passes the null on, and `Build` hands it straight to `ValidateNoMismatchedAttributes` (`PipelineBuilder.cs:76-77`).
3. `CheckAttributeKinds` dereferences `method!` (`PipelineAttributeReader.cs:99`). The NRE is caught as an `AttributeReadFailure` and rethrown unchanged through `ExceptionDispatchInfo`.
4. `QueryProcessor.Execute` calls `Build` outside its `try` (`QueryProcessor.cs:77`), so the raw NRE reaches the caller.

## Evidence
- [x] **Code-trace** (Plan sub-agent, opus; key sites re-checked by the main agent):
  1. `PipelineAttributeReader.cs:45-46`: `GetMethod(EXECUTE_METHOD_NAME)` with no binding flags finds public members only.
  2. `PipelineAttributeReader.cs:137-147`: `Lookup` wraps only exceptions. A null return becomes `ExecuteMethodLookup(null, null)`.
  3. `PipelineBuilder.cs:213-217`: `ResolveMethod` calls `ThrowIfFailed()`, which is `Failure?.Rethrow()`, so it does nothing here and returns null.
  4. `PipelineBuilder.cs:76-77`: there is no guard. Compare async at `:135-136` and stream at `:322-323`.
  5. `PipelineBuilder.cs:221`, then `PipelineAttributeReader.cs:99`: `method!.GetCustomAttributes(...)` throws the NRE.
  6. `PipelineAttributeReader.cs:106-108` stores it, and `PipelineBuilder.cs:222` rethrows it with `ExceptionDispatchInfo`, so the type and stack are kept.
  7. `QueryProcessor.cs:77`: `Build` runs before the `try` at `:79`, so nothing translates the exception.
  8. Attempted refutation: no memoised path bypasses `Build`. ADR 0016 is not implemented, and `PipelineBuilder` is constructed only at `QueryProcessor.cs:67,104,148`.
- [x] **Red repro** (main agent, scratch console app, `ProjectReference` to `src/Paramore.Darker`, net11.0, plain JIT, branch at `fefa746`). Each handler implements its interface's execute member explicitly.

  | Path | Result |
  |---|---|
  | Sync, `IQueryHandler<SQ,string>` | `System.NullReferenceException`; top frame `PipelineAttributeReader.CheckAttributeKinds(...)` at `PipelineAttributeReader.cs:99` |
  | Async, `IQueryHandlerAsync<AQ,string>` | `ConfigurationException`: "Handler ExplicitAsync does not implement ExecuteAsync. Register an async handler or use Execute instead." (`PipelineBuilder.cs:136`) |
  | Stream, `IStreamQueryHandler<StQ,string>` | `ConfigurationException`: "Handler ExplicitStream does not implement a stream ExecuteAsync(TQuery, CancellationToken). Ensure it implements IStreamQueryHandler." (`PipelineBuilder.cs:323`) |

  - This confirms predictions 1 and 2.
  - The probe's handlers are in the global namespace, so `FullName` prints the short name. That explains the short name quoted in the issue.

**Suggested-fix assessment**
1. **Sync null guard throwing `ConfigurationException`: CONFIRMED.**
   - Place it between `PipelineBuilder.cs:76` and `:77`. One guard covers all three later null dereferences: `ValidateNoMismatchedAttributes`, `GetDecorators` (`ReadDecorators`, `PipelineAttributeReader.cs:75`), and the sink's `Invoke` (`:90`).
   - Name the handler by `handlerType.FullName`, as every `ConfigurationException` in the builder does (`:136,239,258,323,417`).
2. **Reword all three messages: PARTIAL.**
   - The current wording misleads when the method is explicit or trimmed.
   - The async message is accurate in one real case: `ResolveHandlerAsync` falls back to the sync registry (`PipelineBuilder.cs:265-266`), so a genuine sync handler reaches `FindExecuteAsync`, which returns null. There, "use Execute instead" is right.
   - So rewording has to tell the two causes apart. The validator already does this (`PipelineValidation/QueryPipelineRules.cs:198-211`, `ImplementsPathInterface`), and its wording could be reused.
   - Rewording breaks no tests: neither message text appears under `test/`.
3. **Support explicit `Execute` via the interface: out of scope (design change).**
   - It is feasible with `FindHandlerInterfaceMethod` (`PipelineBuilder.cs:204-210`). But decorator attributes sit on the class's private explicit method, not on the interface method, so the attribute read would need `GetInterfaceMap`.
   - It would also change the validator's `HasPathMethod` and `HandlerMatchesPath` advice.

## Scope Notes
1. **No other parity gaps.** The async and stream guards (`PipelineBuilder.cs:135`, `:322`) run before `ValidateNoMismatchedAttributes` (`:138`, `:325`) and before decorator reading. Stream handlers cannot reach sync `Build`: the sync registry rejects non-`IQueryHandler<,>` candidates, so the lookup fails earlier with "No sync handler registered" (`:232`).
2. **The async and stream guards have no tests.** No `QueryProcessor` test exercises an explicit `ExecuteAsync` or a stream `ExecuteAsync`. `TestDoubles/ExplicitInterfaceAsyncHandler.cs` is used only by the validator test. The regression test could cover all three paths, so the parity is pinned.
3. **The validator is not affected.** `PipelineDescriber.cs:73` and `:76-77` null-guard before `ReadDecorators` and `CheckAttributeKinds`. The path readers (`PipelineValidation/*PipelinePathReader.cs:49`) feed only `PipelineDescriber`. The builder is the only gap.
4. **Span leak on any build-time failure. Pre-existing, all three paths, out of scope, worth its own issue.**
   - `QueryProcessor.cs:73` (sync) and `:113` (async) create the span before `Build`/`BuildAsync`, outside the `try`/`finally` that calls `EndSpan`. The stream `BuildStream` (`:161`) is likewise outside its inner `try`.
   - `DarkerTracer.CreateQuerySpan` sets `Activity.Current` (`Observability/DarkerTracer.cs:64`), and only `EndSpan` restores it.
   - So any build-time `ConfigurationException` leaves the span un-ended, without the exception recorded, and as the ambient `Activity.Current`. The fix does not make this worse.
5. **The sync and async lookups match by name only. Out of scope, note only.**
   - `FindExecute` and `FindExecuteAsync` match on name, while `FindStreamExecuteAsync` matches on signature (`PipelineAttributeReader.cs:46,50` vs `:54`).
   - A handler with an explicit `IQueryHandler<Q,R>.Execute` plus an unrelated public `Execute(Other)` picks the wrong method. The guard does not fire, and `Invoke` fails with an `ArgumentException` (`PipelineBuilder.cs:90`).
6. **The fallback interface lookup can return null** (`PipelineBuilder.cs:205-210`). It then NREs only when a decorator invokes the fallback. This is reachable only via a custom registry. Low priority; it relates to #413/#410.
7. **ADR 0016 memoisation is unimplemented.** If it is built later, the guard must come before any cache fill keyed on the method.

## Regression Test
**Decisions at the test gate (user)**
- **Clear error, not support for explicit `Execute`.** The sync pipeline throws a `ConfigurationException`, as async and stream do. Supporting explicit `Execute` would be a design change for `/spec`.
- **Distinguish the causes, on all three paths.**
  - When the handler implements the path interface but has no public method, the message reuses the validator's wording: `'{interface}' only explicitly`, plus advice to implement the method publicly and a pointer to running pipeline validation at startup.
  - When the handler does not implement the path interface (async via the sync-registry fallback), the current text stays.
- **Cover sync, async and stream**, at runtime and in the validator.
- **Defensive policy (user).** A possible configuration error should also be caught by `ValidatePipelines`, not only at runtime. A probe showed `HandlerMatchesPath` already reports explicit handlers on all three paths, but only async was tested. So the validator gets characterisation tests and no production change.

**Runtime tests: RED, uncommitted until `/bugfix:fix`**

All in `test/Paramore.Darker.Core.Tests/`. Each asserts a `ConfigurationException`, the handler's `FullName`, and the fragment `'{interface}' only explicitly`.

- `When_a_sync_handler_implements_execute_explicitly_should_throw_ConfigurationException.cs`
  - Today it gets a `NullReferenceException` from `CheckAttributeKinds` (`PipelineAttributeReader.cs:99`).
  - Double: `TestDoubles/ExplicitInterfaceSyncHandler.cs` (new).
- `When_an_async_handler_implements_execute_explicitly_should_throw_ConfigurationException.cs`
  - The exception type and the handler name pass. It fails on the `'IQueryHandlerAsync<,>' only explicitly` fragment, because today's message says "does not implement ExecuteAsync".
  - Double: the existing `TestDoubles/ExplicitInterfaceAsyncHandler.cs`.
- `When_a_stream_handler_implements_execute_explicitly_should_throw_ConfigurationException.cs`
  - The exception type and the handler name pass. It fails on the `'IStreamQueryHandler<,>' only explicitly` fragment, because today's message says "Ensure it implements IStreamQueryHandler".
  - Double: `TestDoubles/ExplicitInterfaceStreamHandler.cs` (new).

**Validator characterisation tests: committed `a5b5062` (`test:`)**

Both in `test/Paramore.Darker.Core.Tests/PipelineValidation/`.

- `When_a_sync_handler_implements_the_interface_explicitly_should_report_handler_matches_path_with_advice.cs`
- `When_a_stream_handler_implements_the_interface_explicitly_should_report_handler_matches_path_with_advice.cs`

How RED was observed:
- Both tests were green on arrival.
- I added `description.Path == PipelinePath.Sync ||` (then `Stream`) to the `HandlerMatchesPath` predicate in `QueryPipelineRules.cs`. Each test then failed at `Single(e => e.RuleId == HandlerMatchesPath)` with "Sequence contains no matching element".
- I reverted from a scratchpad copy. `git diff -- src` was empty afterwards.

**Full suite at `a5b5062`**
- `Build succeeded`.
- `Core.Tests` 388/391 on each of net8.0 to net11.0. The only failures are the three RED runtime tests.
- All other test projects pass.

## Fix
**Summary:** sync `Build` now throws a `ConfigurationException` when `Execute` cannot be found. On all three paths, a handler that implements its path interface but has no public method gets the validator's "only explicitly" advice instead of "does not implement".

**`d7568c8` `refactor:`**
- Extracts `FindHandlerInterface(handlerType, definition, queryType)` from `FindHandlerInterfaceMethod` in `src/Paramore.Darker/PipelineBuilder.cs`.
- No behaviour change: the suite was unchanged (only the three pending regression tests failed).

**`fix:` (`src/Paramore.Darker/PipelineBuilder.cs`)**
- `Build`: a null guard after `FindExecute`, before `ValidateNoMismatchedAttributes`.
- `BuildAsync` and `BuildStream`: their existing guards now go through the same message builder.
- `MissingPathMethod(handlerType, definition, queryType, methodName, notImplementedMessage)`:
  - If `FindHandlerInterface` finds the closed path interface, it says: *"Handler {FullName} implements '{Open(interface)}' only explicitly, or its public {method} was trimmed, so it has no public {method}. Implement {method} publicly. Validate pipelines at startup (ValidatePipelines) to report this before the first query."*
  - Otherwise it keeps the path's current text. Sync gets a new equivalent: "does not implement Execute. Ensure it implements IQueryHandler."

**Checks**
- **Regression tests:** all three pass (net11.0, `--filter ExplicitExecute`).
- **Probe (scratch app), after the fix:**
  - Sync, async and stream each give the new message.
  - A public sync `QueryHandler<,>` called through `ExecuteAsync` is unaffected. It still fails earlier with "No async handler registered … use Execute instead". The "does not implement ExecuteAsync" text is now reached only when no async registry is configured.
- **Trim analysis** (`dotnet build src/Paramore.Darker -c Release -f net11.0 --no-incremental`):
  - 19 unique `ILnnnn` warning sites, both before and after the fix.
  - The same set by file and code. The new interface check reuses the existing `GetInterfaces` site.

## Verification
Full suite after the fix (`dotnet build Darker.Filter.slnf -c Release`, then `dotnet test Darker.Filter.slnf -c Release --no-build`):
- `Build succeeded`, 0 errors.
- 36 of 36 test runs passed (9 projects × net8.0 to net11.0). `Core.Tests` is 391/391 on each framework; it was 388/391 before the fix.
