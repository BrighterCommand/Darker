# Bugfix: Reflective fallback delegates do not unwrap TargetInvocationException for direct IQueryHandler<,> / IQueryHandlerAsync<,> implementers

**Linked Issue**: #413
**Status**: Verified

## Symptom
A handler's `Fallback` / `FallbackAsync` can throw. When it does, decorators that sit *outside* the fallback decorator (lower step numbers, so they run outermost) receive a `System.Reflection.TargetInvocationException` instead of the real exception. This only happens when the handler implements `IQueryHandler<,>` / `IQueryHandlerAsync<,>` directly. Handlers that derive from `QueryHandler<,>` / `QueryHandlerAsync<,>` are not affected, because #411 calls their fallback without reflection.

Probe from the issue (net11.0, JIT, at `fefa746`): a recording decorator at step 1, `[FallbackPolicy]` at step 2, `Execute` throws `InvalidOperationException`, `Fallback` throws `NotSupportedException`.

| Handler | Outer decorator sees | Caller sees |
|---|---|---|
| implements `IQueryHandler<Q, string>` directly | `TargetInvocationException` | `NotSupportedException` |
| derives from `QueryHandler<Q, string>` | `NotSupportedException` | `NotSupportedException` |

Callers never see the wrapper. `QueryProcessor` removes it at its boundary: `src/Paramore.Darker/QueryProcessor.cs:83-86` (sync) and `:124-127` (async) use `catch (TargetInvocationException ex) when (ex.InnerException != null)` plus `ExceptionDispatchInfo.Capture(...).Throw()`. The problem is only *inside* the pipeline. For example:
- an outer retry or resilience decorator that filters on exception type will not match;
- an outer logging decorator records the wrapper instead of the real exception.

The result is that what decorators observe now depends on the kind of handler.

## Suspected Location
Line numbers are as of `13aa0ca` (the issue's `:189` / `:200` are from `fefa746`).

**The reflective fallback delegates (the suspects):**
- `src/Paramore.Darker/PipelineBuilder.cs:188-195`, `ResolveFallback` (sync).
  - `:190-191` is the non-reflective path for `IAmAFallbackTarget<TResult>` (that is, `QueryHandler<,>`, see `src/Paramore.Darker/QueryHandler.cs:6,21`). It returns `target.Fallback` directly.
  - `:193-194` is the reflective path for every other handler:
    `return r => (TResult)fallbackMethodInfo.Invoke(_handler, new object[] { r });`
    There is no `try`/`catch`, so it does not unwrap.
- `src/Paramore.Darker/PipelineBuilder.cs:199-206`, `ResolveFallbackAsync` (async).
  - `:201-202` is the non-reflective path for `IAmAFallbackTargetAsync<TResult>` (`QueryHandlerAsync<,>`, see `src/Paramore.Darker/QueryHandlerAsync.cs:8,23-24`).
  - `:204-205` is the reflective path:
    `return (r, ct) => (Task<TResult>)fallbackMethodInfo.Invoke(_handler, new object[] { r, ct });`
    It does not unwrap either.
- `src/Paramore.Darker/PipelineBuilder.cs:209-210`, `FindHandlerInterfaceMethod`. It resolves the interface `MethodInfo`, so `Invoke` dispatches to an explicit or implicit implementation.

**For contrast, the Execute sinks do unwrap:**
- Sync sink: `src/Paramore.Darker/PipelineBuilder.cs:92-100`. `Invoke` is at `:94`, and `catch (TargetInvocationException ex) when (ex.InnerException != null)` plus `ExceptionDispatchInfo.Capture(ex.InnerException).Throw()` is at `:96-98`.
- Async sink: `src/Paramore.Darker/PipelineBuilder.cs:155-163`. `Invoke` is at `:157` and the unwrap is at `:159-161`.

**Where the fallback delegate is wired in and called.** It is *not* passed through the context bag. It is a closure argument passed down the decorator chain:
- Sync: built at `PipelineBuilder.cs:105` and passed to every decorator at `:115` (`decorator.Execute(r, next, fallback)`). The contract is `src/Paramore.Darker/IQueryHandlerDecorator.cs:15`.
- Async: built at `PipelineBuilder.cs:168` and passed at `:179` (`decorator.ExecuteAsync(r, next, fallback, ct)`). The contract is `src/Paramore.Darker/IQueryHandlerDecoratorAsync.cs:12`.
- `src/Paramore.Darker/Policies/Handlers/FallbackPolicyDecorator.cs:25-44`. `next(query)` runs inside `try` (`:30`). In `catch`, if the exception matches the filter, the decorator stores the *cause* in the bag, `Context.Bag.Add(CauseOfFallbackException, ex)` (`:37`, key `"Fallback_Exception_Cause"` at `:12`), then calls `return fallback(query);` (`:38`). That call is inside the `catch` block, not inside the `try`, so anything the fallback throws, wrapper included, goes straight to the outer decorators.
- `src/Paramore.Darker/Policies/Handlers/FallbackPolicyDecoratorAsync.cs:27-49`. Same shape: `await next(...)` at `:35`, bag write at `:42`, `return await fallback(query, cancellationToken).ConfigureAwait(false);` at `:43`.
- The context bag carries only the *cause* exception (from `Execute`), never the fallback delegate.

**Stream path: N/A.**
- `src/Paramore.Darker/StreamQueryHandler.cs:8` documents "No Fallback method — a partially-emitted stream has no meaningful fallback value."
- `BuildStream` (`src/Paramore.Darker/PipelineBuilder.cs:324-374`) never resolves a fallback. Stream decorators are called as `dec.Execute(r, next, ct)` (`:369`) with no fallback argument.
- The stream sink deliberately does not unwrap (comment at `:351`, `Invoke` at `:357`), because iterator bodies run on `MoveNextAsync`, not inside `Invoke`.

**Existing tests touching fallback for direct implementers.** Both cover only the success path; neither has a throwing fallback or an outer decorator:
- Sync: `test/Paramore.Darker.Core.Tests/When_a_sync_handler_implements_fallback_explicitly_should_return_the_fallback_result.cs:12-30`, using `test/Paramore.Darker.Core.Tests/TestDoubles/ExplicitFallbackSyncHandler.cs:7-16` (implements `IQueryHandler<,>` directly, `[FallbackPolicy(1)]` at `:11`, explicit `Fallback` at `:15-16` that returns a value).
- Async: `test/Paramore.Darker.Core.Tests/When_an_async_handler_implements_fallback_explicitly_should_return_the_fallback_result.cs`, using `test/Paramore.Darker.Core.Tests/TestDoubles/ExplicitFallbackAsyncHandler.cs:9-19` (implements `IQueryHandlerAsync<,>` directly, `[FallbackPolicyAttributeAsync(1)]` at `:13`, explicit `FallbackAsync` at `:17-19` returning `Task.FromResult(...)`).

**Related tests on base-class handlers** (they do not exercise the reflective path):
- `test/Paramore.Darker.Core.Tests/PipelineBuilderExceptionTests.cs:68-81`, `ShouldThrowExceptionWhenFallbackThrowsException`, via `TestDoubles/FallbackExceptionQuery.cs:11-20` (a `QueryHandler<,>` whose `Fallback` throws `NotSupportedException`). It asserts only at the `QueryProcessor` boundary, where the unwrap already happens.
- `PipelineBuilderExceptionTests.cs:55-63` checks the `when (ex.InnerException != null)` filter, via `TestDoubles/NullInnerExceptionQuery.cs:11`. Any fix that copies the sink pattern must keep that filter.
- `test/Paramore.Darker.Core.Tests/Decorators/FallbackPolicyTests.cs` (`:32`, `:58`, `:84`) uses base-class handlers and checks the `Fallback_Exception_Cause` bag entry.
- `test/Paramore.Darker.Core.Tests/TestDoubles/RecordingDecoratorFactory.cs:18` is an existing recording-decorator helper that could serve as the outer observer.

## Root-Cause Hypothesis
**H1 (primary).** For handlers that do not implement `IAmAFallbackTarget<TResult>` / `IAmAFallbackTargetAsync<TResult>`, `ResolveFallback` / `ResolveFallbackAsync` return a lambda that calls `MethodInfo.Invoke` (`PipelineBuilder.cs:194`, `:205`) without the `TargetInvocationException` unwrap the Execute sinks use (`:96-98`, `:159-161`). `FallbackPolicyDecorator` calls `fallback(query)` from inside its `catch` block (`FallbackPolicyDecorator.cs:38`, `FallbackPolicyDecoratorAsync.cs:43`), so whatever the fallback throws reaches every outer decorator still wrapped. It is only unwrapped later, at `QueryProcessor.cs:83` / `:124`. Since #411, base-class handlers skip `Invoke` (`:190-191`, `:201-202`), so they surface the real exception, and behaviour now differs by handler kind.

**H2 (async throw shapes; to probe).**
- `fallbackMethodInfo.Invoke` at `:205` returns the method's `Task<TResult>`. `TargetInvocationException` wraps only exceptions thrown *synchronously* while `Invoke` runs.
- A non-`async` `FallbackAsync` that throws before returning a Task (e.g. `=> throw new NotSupportedException()`) makes `Invoke` throw `TargetInvocationException` synchronously out of the delegate, and `FallbackPolicyDecoratorAsync.cs:43` propagates it to outer decorators.
- An `async` `FallbackAsync` (even one that throws before its first `await`), or one returning `Task.FromException(...)`, stores the exception in the Task. `Invoke` does not wrap it, and the `await` at `:43` rethrows the real exception, so outer decorators would *not* see a wrapper.
- Consequence for the test: the async regression test must use a non-`async` `FallbackAsync` that throws synchronously, or it will pass on unfixed code. The sync path is always wrapped.

**Suggested fix from the issue (UNVERIFIED — to be proven or refuted in /bugfix:confirm).** Make the reflective fallback delegates at `PipelineBuilder.cs:194` and `:205` unwrap `TargetInvocationException` the way the sinks do: `catch (TargetInvocationException ex) when (ex.InnerException != null)` plus `ExceptionDispatchInfo.Capture(ex.InnerException).Throw()`. Decorators would then see the same exception whatever the handler kind.

Points to carry into confirm:
- This changes what outer decorators observe for direct implementers. Call it out in the PR.
- #410 may later replace the reflective path for direct implementers; every #410 option keeps a reflective path for `Type`-based registration, so this fix survives it.
- No `ConfigurationException` is added or changed, so the "be defensive" `ValidatePipelines` check does not apply.
- Prior analysis is in `bugfixes/0001-il2080-fallback-trim-warning/bugfix.md`: Scope Note 5 (`:164`), the triage note (`:69`) and the Fix section (`:214`).

## Confirmed Root Cause
**Verdict: CONFIRMED (H1 and H2).**

`ResolveFallback` and `ResolveFallbackAsync` (`src/Paramore.Darker/PipelineBuilder.cs:188-195`, `:199-206`) use a non-reflective path for `IAmAFallbackTarget(Async)<TResult>` (`:190-191`, `:201-202`). For every other handler they return a bare `MethodInfo.Invoke` lambda (`:194` sync, `:205` async) with no `catch (TargetInvocationException ex) when (ex.InnerException != null)` + `ExceptionDispatchInfo.Capture(ex.InnerException).Throw()`, which the Execute sinks have (`:96-99`, `:159-162`). `FallbackPolicyDecorator(Async)` calls the delegate from its `catch` block (`FallbackPolicyDecorator.cs:38`, `FallbackPolicyDecoratorAsync.cs:43`), so the wrapper passes up the chain untouched. Only `QueryProcessor.cs:83-86` / `:124-127` remove it, at the caller boundary.

On the async path, only a non-`async` `FallbackAsync` that throws synchronously is wrapped. An `async` `FallbackAsync` (even one that throws before its first `await`) or one returning `Task.FromException` puts the exception in the Task. `Invoke` then returns normally, and the `await` at `FallbackPolicyDecoratorAsync.cs:43` rethrows the real exception.

## Evidence
- [x] **Red repro** (scratch console app, `ProjectReference` to `src/Paramore.Darker`, net11.0, JIT, on `13aa0ca`). An outer recording decorator at step 1 catches and records what `next` throws; `[FallbackPolicy(2)]` / `[FallbackPolicyAttributeAsync(2)]`; `Execute(Async)` throws `InvalidOperationException`; the fallback throws `NotSupportedException`:

  | Handler | Outer decorator sees | Caller sees |
  |---|---|---|
  | sync, implements `IQueryHandler<,>` directly | **`TargetInvocationException`** | `NotSupportedException` |
  | sync, derives from `QueryHandler<,>` | `NotSupportedException` | `NotSupportedException` |
  | async, direct, non-`async` `FallbackAsync => throw …` | **`TargetInvocationException`** | `NotSupportedException` |
  | async, direct, `async` `FallbackAsync` | `NotSupportedException` | `NotSupportedException` |
  | async, direct, `Task.FromException` | `NotSupportedException` | `NotSupportedException` |
  | async, derives from `QueryHandlerAsync<,>` | `NotSupportedException` | `NotSupportedException` |

  The failing assertion for the regression test is on the exception the outer decorator observes: it should be `NotSupportedException`, and today it is `TargetInvocationException`. A caller-side `Should.Throw<NotSupportedException>` already passes (because of `QueryProcessor`), so it is not a valid red test.
- [x] **Code-trace** (sub-agent):
  1. `PipelineAttributeReader.cs:78` sorts by descending `Step`, and `PipelineBuilder.cs:107-116` / `:170-180` wrap `pipeline.Last()` each time, so the lowest step is outermost.
  2. The fallback is built once (`:105` / `:168`) and passed to every decorator as an argument (`:115`, `:179`), not through the context bag.
  3. The sink unwraps `Execute`'s exception (`:96-99`). `FallbackPolicyDecorator` matches it (`:34`), stores the cause (`:37`) and calls `fallback(query)` (`:38`).
  4. For a direct implementer the fallback is the `:194` lambda, and `Invoke` wraps the throw.
  5. The decorator lambda (`:112-116`) does not catch, so the step-1 decorator's `next(...)` throws the wrapper.
  6. `QueryProcessor.cs:83-86` unwraps it for the caller.
  7. Refutation attempt: the only calls to `fallback(` in `src` are `FallbackPolicyDecorator.cs:38` and `FallbackPolicyDecoratorAsync.cs:43`, and nothing in between unwraps.
  8. Async (H2): a synchronous `TargetInvocationException` from the `:205` lambda is raised inside the `async` `FallbackPolicyDecoratorAsync.ExecuteAsync` body. The state machine stores it in the decorator's Task, so an outer decorator that awaits it sees the wrapper.

**Suggested-fix assessment: CONFIRMED.** Wrapping both lambdas at `:194` and `:205` in the sink's `try`/`catch (TargetInvocationException ex) when (ex.InnerException != null)`/`ExceptionDispatchInfo.Capture(ex.InnerException).Throw()` fixes the cause, and there is no other fallback call site.
- The async lambda must stay non-`async`, like the sink at `:155-163`. The rethrow then stays synchronous, which keeps today's timing, and the real exception lands in `FallbackPolicyDecoratorAsync`'s Task with its stack trace kept.
- Keep the `when (ex.InnerException != null)` filter, to match the sink and `QueryProcessor`.
- `BindingFlags.DoNotWrapExceptions` is not an option: it is unavailable on `netstandard2.0` (`Paramore.Darker.csproj:3`), and it would diverge from the sink pattern.

## Scope Notes
1. **Sync/async parity:** both paths are affected and both lambdas need the fix. Sync always wraps; async wraps only for a synchronously-throwing non-`async` `FallbackAsync`, so the async regression test must use that shape or it passes on unfixed code.
2. **Stream path: N/A.** There is no stream fallback (`StreamQueryHandler.cs:8`, `BuildStream` at `PipelineBuilder.cs:324-374`). The stream sink's lack of an unwrap (`:351`, `:357`) is deliberate and tested (`When_stream_handler_throws_during_enumeration_should_surface_original_exception.cs:66-68`).
3. **Other reflective `Invoke` sites:** `PipelineBuilder.cs:94` / `:157` already unwrap. `QueryProcessor.cs:81` / `:122` and `Logging/QueryProcessorBuilderExtensions.cs:22` are delegate calls, not reflection. Only `:194` and `:205` are affected.
4. **Policy and resilience decorators** (`RetryableQueryDecorator(Async)`, `UseResiliencePipelineHandler(Async)`), Caching, Validation and `QueryLoggingDecorator(Async)` receive `fallback` but never call it. They are victims when they are outermost (their policies filter on exception type), not further fault sites. No change is needed there. A sharp real-world case: an outer `[FallbackPolicy(1, typeof(NotSupportedException))]` around an inner `[FallbackPolicy(2)]` today skips its fallback, because `_exceptionTypes.Contains(ex.GetType())` (`FallbackPolicyDecorator.cs:34`) sees `TargetInvocationException`.
5. **No test relies on the wrapper.** The only `TargetInvocationException` assertion is `PipelineBuilderExceptionTests.cs:63` (null inner exception, sink path, unaffected). `ShouldThrowExceptionWhenFallbackThrowsException` (`:68-81`) uses a base-class handler and asserts at the caller.
6. **`QueryProcessor`'s unwrap** (`QueryProcessor.cs:83-86`, `:124-127`) becomes defensive-only after the fix (for example, for third-party decorators that use reflection). Keep it.
7. **Behaviour change to call out in the PR:** outer decorators of direct implementers will see the real exception instead of `TargetInvocationException`. That includes an outer `FallbackPolicyDecorator`, which will store the real exception under `Fallback_Exception_Cause`.
8. **No `ValidatePipelines` / `ConfigurationException` impact.** No configuration error is added or changed. The fix survives #410, because the reflective path stays for `Type`-based registration.
9. Out of scope, pre-existing: a handler that itself throws a `TargetInvocationException` with an inner exception is unwrapped twice (once by the sink, once by `QueryProcessor`). Not filed.

## Regression Test
Both are in `test/Paramore.Darker.Core.Tests/` and were approved at the `/tdd:test-first` gate:
- `When_a_direct_sync_handler_fallback_throws_should_show_outer_decorators_the_fallback_exception.cs` (`ReflectiveFallbackExceptionSyncTests`)
- `When_a_direct_async_handler_fallback_throws_should_show_outer_decorators_the_fallback_exception.cs` (`ReflectiveFallbackExceptionAsyncTests`)

Shape: the handler implements `IQueryHandler(Async)<,>` directly. An observing decorator at step 1 sits outside `[FallbackPolicy(2)]` / `[FallbackPolicyAttributeAsync(2)]`. `Execute(Async)` throws `InvalidOperationException` and the fallback throws `NotSupportedException`. Each test asserts that the caller gets `NotSupportedException` (a guard, already green) and that `observer.Observed` is `NotSupportedException` (the red assertion).

New test doubles in `TestDoubles/`:
- `ExceptionObservingAttribute`, `ExceptionObservingDecorator<,>`, `ExceptionObservingAsyncAttribute`, `ExceptionObservingAsyncDecorator<,>`.
- `ThrowingFallbackSyncHandler` (public `Fallback`).
- `ThrowingFallbackAsyncHandler` (a non-`async` `FallbackAsync => throw …`, the only async shape that is wrapped; see Scope Note 1).

RED observed on net11.0: both tests fail with `observer.Observed should be of type NotSupportedException but was System.Reflection.TargetInvocationException`.

## Fix
`src/Paramore.Darker/PipelineBuilder.cs`: the reflective fallback functions in `ResolveFallback` (sync) and `ResolveFallbackAsync` (async) now wrap `fallbackMethodInfo.Invoke` in the same `try` / `catch (TargetInvocationException ex) when (ex.InnerException != null)` / `ExceptionDispatchInfo.Capture(ex.InnerException).Throw()` that the Execute sinks use. Decorators outside `[FallbackPolicy]` now see the fallback's own exception whatever the kind of handler.
- The async function stays non-`async`, so a synchronous throw stays synchronous, as in the sink.
- The base-class path (`IAmAFallbackTarget(Async)`) and the stream path are unchanged.
- Straight fix with no tidy-first step: the change is localized to the two functions.

Result: both regression tests and the two existing explicit-fallback tests pass on net8.0, net9.0, net10.0 and net11.0.

**Behaviour change (for the PR):** for handlers that implement `IQueryHandler(Async)<,>` directly, outer decorators now get the real exception instead of `TargetInvocationException`. That includes an outer `FallbackPolicyDecorator`, whose exception filter can now match. Callers are unaffected, because `QueryProcessor` already unwrapped.
