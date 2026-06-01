# Review: design — 006-json_serializer

**Date**: 2026-06-01
**Threshold**: 60
**Verdict**: NEEDS WORK

> Note: This phase is already approved (`.design-approved` marker exists, ADR Status is `Accepted`). The user requested a post-approval adversarial review.

## Findings

### 1. The "single call-site discipline" is contradicted by the ADR's own text (Score: 78)

The ADR pins `Paramore.Darker.Logging.QueryProcessorBuilderExtensions.AddJsonQueryLogging<TBuilder>` as the *sole* site where the consumer callback is invoked against `QueryLoggingJsonOptions.Options`. Step 5 of Decision says "guarantees the callback runs exactly once per consumer call". However, the requirements doc (FR4) describes both the `JsonQueryLogging(IBuildTheQueryProcessor, …)` overload and the DI `AddJsonQueryLogging(IDarkerHandlerBuilder, …)` overload as "delegates to canonical". For these to forward to the canonical generic site, they must call `QueryProcessorBuilderExtensions.AddJsonQueryLogging<TBuilder>(builder, configure)` — and the generic constraint is `where TBuilder : IQueryProcessorExtensionBuilder`. That works for `IDarkerHandlerBuilder` (it implements `IQueryProcessorExtensionBuilder`, verified in `src/Paramore.Darker.Extensions.DependencyInjection/IDarkerHandlerBuilder.cs:8`), but **`IBuildTheQueryProcessor` does NOT extend `IQueryProcessorExtensionBuilder`** (verified `src/Paramore.Darker/Builder/IBuildTheQueryProcessor.cs` only contains `IQueryProcessor Build();`). The existing builder path therefore must cast `IBuildTheQueryProcessor` to the concrete `QueryProcessorBuilder` (which does implement both — verified `src/Paramore.Darker/Builder/QueryProcessorBuilder.cs:7`). The ADR's Architecture Overview and Key Components glosses over this asymmetric path — the cast pattern is the existing precedent (`Policies/QueryProcessorBuilderExtensions.cs:11-14`), but the ADR omits the cast and `NotSupportedException` path entirely, including the fact that consumers using a custom `IBuildTheQueryProcessor` cannot use the builder surface.

**Evidence**: ADR step 5 (line 86): "*This guarantees the callback runs exactly once per consumer call, regardless of which surface they used*"; the existing precedent for the cast is `src/Paramore.Darker/Policies/QueryProcessorBuilderExtensions.cs:11-14` which throws `NotSupportedException` if the builder isn't the concrete `QueryProcessorBuilder`. The ADR's Key Components section calls `JsonQueryLogging` "a thin forwarder" without mentioning the cast/NotSupportedException semantics.

**Recommendation**: Add a sentence to "Key Components → `QueryProcessorBuilderExtensions`" calling out that the `IBuildTheQueryProcessor` overload casts to concrete `QueryProcessorBuilder` and throws `NotSupportedException` for custom builders. This is a real, documented limitation that consumers will hit.

---

### 2. Mutable-global "safe-by-self-lock" claim ignores cross-test/cross-host parallel hazards (Score: 75)

The ADR's central architectural defence is that `JsonSerializerOptions` self-locks on first use, making the static "safe at runtime under the canonical usage pattern" (Forces section, line 40). The Consequences section's *Negative* bullet does mention "No per-app or per-test isolation" (line 259) and points consumers at `DisableParallelization`, but the **Decision** section (steps 1–10) and the **Architecture Overview** lifetime diagram present the self-lock as the load-bearing safety mechanism. The hazard is not what self-lock catches; it is what it does NOT catch:

- Two `WebApplicationFactory<>` hosts in parallel: BOTH bootstrap try to call `AddJsonQueryLogging(o => …)`. The first call to `Serialize` from EITHER host locks the options for BOTH. Whichever host's startup callback runs second after that lock throws `InvalidOperationException` mid-startup — not at a query, at host construction.
- A consumer who configures options at startup via `AddJsonQueryLogging(o => …)` and then attempts to alter `QueryLoggingJsonOptions.Options.MaxDepth` from a feature-flag-driven runtime path: silent process-wide effect on every other tenant/host.
- The ADR says "Concurrent mutation during query execution is **undefined behaviour**" (step 8). "Undefined behaviour" in a logging hot path is a very bad architectural commitment — at minimum it should say *what the consumer should expect* (`InvalidOperationException` per self-lock, or torn writes per reference assignment) rather than punt to "undefined."

The Decision section claims the self-lock makes the global safe; the Negative section quietly admits parallel hosts break. These two framings are internally inconsistent.

**Evidence**: ADR line 40 (Forces): "*This makes a static global safe at runtime under the canonical usage pattern*"; ADR line 92 (Decision step 8): "*Concurrent mutation during query execution is undefined behaviour*"; ADR line 259 (Negative): "*Two `WebApplicationFactory<TStartup>` hosts in the same process share `QueryLoggingJsonOptions.Options`. Consumer test suites running parallel integration hosts must `DisableParallelization`*".

**Recommendation**: Reframe the safety argument: self-lock guards against *post-startup mutation* on a single-host process, NOT against multi-host parallelism. Either (a) explicitly enumerate the failure mode in Decision (not just in Consequences), or (b) demote the self-lock from "safety mechanism" to "useful failure signal" and lean on the startup-only contract as the actual safety story. Replace "undefined behaviour" with a specific exception/torn-write claim.

---

### 3. `UnconditionalSuppressMessage` semantics likely don't propagate as claimed (Score: 72)

Step 3 of the Decision says: "Suppress `IL2026` and `IL3050` at the `Serialize<T>` method with `UnconditionalSuppressMessage` attributes" — and FR13 pins the attributes onto the `Serialize<T>` method body. The intent is that the decorator's call to `JsonSerializer.Serialize<T>(value, ...)` is suppressed because `Serialize<T>` carries the attributes. But the `Serialize<T>` method *itself* is the call site of `JsonSerializer.Serialize` — and `UnconditionalSuppressMessage` placed on the method DOES suppress warnings that fire from code inside that method. So far, so good. The real problem is what happens to the *callers* of `Serialize<T>`:

`Serialize<T>` accepts `T value` as an unconstrained generic. When `Execute`/`ExecuteAsync` calls `Serialize(query)` where `TQuery : IQuery<TResult>`, the analyser may propagate IL2026/IL3050 to the caller because `Serialize<T>` is now effectively a `[RequiresUnreferencedCode]`/`[RequiresDynamicCode]` method body that suppresses at-source but does NOT change the method's annotations. Practically, `UnconditionalSuppressMessage` on the called method does suppress the analyser warning inside *that method*, but the warning that fires at the call to `JsonSerializer.Serialize` IS suppressed (because it fires inside `Serialize<T>`'s body). The chain works — but the ADR's claim that the suppression is "localised to the known-safe call" elides the fact that the generic `T` makes the entire `Execute<TQuery>` method body suspect from an analyser PoV. AC4 in requirements promises "any `IL3xxx` or `IL2xxx` warning under `src/Paramore.Darker/Logging/`" is a CI failure with an explicit allow-list — that allow-list names only the two `Serialize` methods. If the analyser warns at `Execute`'s call to `Serialize`, that's a non-allow-listed warning and the build fails. The ADR doesn't acknowledge this risk.

**Evidence**: ADR line 82 (Decision step 3) + lines 132-141 of `requirements.md` (FR13). AC4 in requirements (`requirements.md:256-258`) names ONLY `Serialize` on both decorators in the allow-list.

**Recommendation**: Either (a) verify empirically (build the AOT publish with attributes in place and confirm `Execute`/`ExecuteAsync` don't also need suppressions) and add a sentence to Decision step 3 about which methods carry the attributes, or (b) expand the allow-list pre-emptively and document the risk. The ADR should not commit to "suppressions only on `Serialize`" without proof.

---

### 4. ADR characterises Newtonsoft.Json as having transitive deps it does not have (Score: 68)

Forces, line 26: "*`Newtonsoft.Json` is widely-used but heavy and adds a transitive surface (`Newtonsoft.Json` plus its dependencies on `netstandard2.0`)*". Newtonsoft.Json 13.x has **zero** transitive package dependencies on any TFM (it's a self-contained DLL). The Positive consequence line 251 then claims "*`Newtonsoft.Json` pulls a similar-sized cluster*" — which directly contradicts the actual NuGet package, which pulls nothing. By comparison, `System.Text.Json` on `netstandard2.0` does pull `System.Memory`, `System.Text.Encodings.Web`, `System.Buffers`, `System.Threading.Tasks.Extensions`, etc. So the ADR's "smaller transitive dependency surface on `netstandard2.0`" claim (Positive consequences) is the opposite of true for `netstandard2.0`. The qualitative-direction defence ("the qualitative win is *direction*") is reasonable, but the size argument is empirically backwards.

**Evidence**: ADR lines 26 and 251 claim Newtonsoft has transitive deps; verify on NuGet that `Newtonsoft.Json` 13.0.4 declares no package dependencies (it has only target-framework references).

**Recommendation**: Strike the "plus its dependencies on `netstandard2.0`" parenthetical (line 26) and the "Newtonsoft.Json pulls a similar-sized cluster" sentence (line 251). Keep the ecosystem-direction argument and drop the false size-equivalence claim, OR — more honestly — admit that on `netstandard2.0` the swap *increases* the transitive surface in exchange for ecosystem alignment and AOT story.

---

### 5. The "direct assignment" path's information loss is acknowledged but the ADR sells it as costless (Score: 67)

Forces, line 56: "*Supporting both costs nothing extra (one settable property + one callback hook); blocking direct assignment would require a sealed instance and lose flexibility for low gain.*" The "costs nothing extra" claim is empirically false:

1. Direct assignment silently drops `ReferenceHandler.IgnoreCycles`. Consumers who use this path AND have an EF-Core-backed query will get a `JsonException` on the logging hot path — exactly the diagnostic outcome the ADR's Force-block (line 50) calls out as bad.
2. The release notes must carry a *separate* migration entry calling this out (and per requirements DoD, they do — see `requirements.md:283-290`). That's not zero cost; that's deferred cost to the consumer.
3. The release-notes migration is the ONLY mechanism that tells the consumer to re-apply `IgnoreCycles`. There's no compile-time guard, no setter-side warning, no logging of "you have direct-assigned options without IgnoreCycles". A consumer who copy-pastes the `new JsonSerializerOptions { … }` pattern from a blog post and forgets to re-apply IgnoreCycles silently regresses.

The Decision (step 7) says the direct-assignment path is "supported but documented as the 'you own all the defaults' path". "Documented" is the entire defence. That's load-bearing on the consumer reading the release notes.

**Evidence**: ADR line 56 (claim of "costs nothing extra"); ADR line 90 (Decision step 7: "Direct assignment drops `ReferenceHandler.IgnoreCycles`"); ADR line 50 (Forces: serialisation throws "mask the underlying query result with a logging failure — a bad diagnostic outcome").

**Recommendation**: Drop "costs nothing extra"; honestly state the cost (consumers using direct assignment can regress cycle handling and the only safety net is documentation). Consider either (a) wrapping the setter to log a warning when assigned options don't have `ReferenceHandler.IgnoreCycles`, or (b) explicitly stating "we accept this regression risk because Brighter parity matters more than the safety guard."

---

### 6. Brighter parity is asserted but the Brighter `JsonSerialisationOptions` initialiser is not cross-checked (Score: 65)

The ADR uses "Brighter parity" as a top-tier load-bearing force (lines 28, 36, 60, 217, 220). It claims the static class shape matches Brighter exactly. But the ADR's Decision (step 1) prescribes:

```csharp
public static JsonSerializerOptions Options { get; set; }
// default: new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles }
```

The ADR claims this *mirrors* Brighter, but does not show what Brighter's default initialiser actually contains. If Brighter's `JsonSerialisationOptions.Options` default does NOT include `ReferenceHandler.IgnoreCycles`, then Darker is **diverging** from Brighter on the very default this ADR pins as the safety-critical setting (FR3 / Force line 50). The ADR cannot use Brighter parity as a force on the surface shape AND then quietly diverge on the default content, without explicitly calling out and justifying the divergence. The Forces section (line 28) explicitly highlights that divergence "would re-introduce divergence in the very place ADR 0011 just closed" — yet the proposed default (`IgnoreCycles`) appears to be a Darker-specific addition.

A reviewer reading just this ADR cannot determine whether the default is or is not Brighter-parity. That's a documentation gap on the load-bearing parity argument.

**Evidence**: ADR line 28 (parity force); ADR Constraints line 60 (mirror Brighter on shape); ADR Decision step 1 (default `IgnoreCycles`); no Brighter source quotation showing Brighter's default initialiser content.

**Recommendation**: Add a line under Decision step 1 or under "Constraints" explicitly stating whether Brighter's `JsonSerialisationOptions.Options` defaults include `ReferenceHandler.IgnoreCycles`. If yes, cite. If no, explicitly justify the Darker-side divergence (e.g. "Darker adds `IgnoreCycles` as a default that Brighter does not, because Darker's query objects are more frequently EF-Core entity graphs").

---

### 7. AOT trim-safety conflation — "publishable" vs "safe at runtime" elided (Score: 63)

The ADR uses two distinct claims interchangeably:

- "**AOT story improved**" / "*first-class AOT story (with documented limitations)*" (Positive consequences and Technology Choices)
- "AOT-publishability" / "AOT-publishable, with documented limitations" (NFR2 in requirements)

NFR2 and OOS11 are honest that AOT *publish* succeeds but *runtime behaviour* may strip properties if the consumer's query types aren't preserved. The ADR's *Decision* (step 3) suppresses IL2026/IL3050 — which is the right thing to do for publish-time analyser noise, but the suppression is a "we know the call site doesn't crash the trimmer" promise, NOT a "the runtime output is correct" promise. The Positive consequences bullet ("AOT story improved") elides this distinction; a reviewer reading only Consequences could reasonably conclude Darker is now AOT-safe. It is not — it is AOT-*publishable* with consumer-side trim-safety obligations.

This conflation is the same one OOS11 and NFR2 carefully separate. The ADR's Consequences should match that level of precision.

**Evidence**: ADR line 249 (Positive: "*AOT story improved*"); ADR line 217 ("*first-class AOT story*"); ADR Risks line 267 ("*AOT publish succeeds (the warnings are suppressed); runtime output may be incomplete*").

**Recommendation**: Rewrite the "AOT story improved" Positive bullet to: "*AOT publish succeeds (warnings suppressed with documented justification); trim-safety remains the consumer's responsibility (per OOS11) and the consumer's escape hatch is a source-generated `JsonSerializerContext`*." The Risk bullet already says this; harmonise the Positive bullet with it.

---

### 8. Decorator-pattern escape hatch is asserted but not verified end-to-end (Score: 62)

The ADR dismisses Alternative 1 (pluggable `IQueryLoggingSerializer` interface) on the grounds that "*a consumer who wants Newtonsoft, MessagePack, or anything else writes a custom decorator and skips `AddJsonQueryLogging()`*" (Forces line 42; Alternatives line 280). This relies on the decorator factory plumbing actually letting a consumer register a custom decorator that *does not* take `JsonSerializerOptions`. Inspection shows:

- `IQueryHandlerDecoratorFactory.Create<T>(Type decoratorType)` returns an `IQueryHandlerDecorator` instance.
- `ServiceProviderHandlerDecoratorFactory` resolves via `_serviceProvider.GetService(decoratorType)` — a consumer's custom decorator can be registered in DI and resolved with whatever constructor parameters they want.
- `SimpleHandlerDecoratorFactory` takes a `Func<Type, IQueryHandlerDecorator>` lambda — consumer fully controls construction.

So the claim is correct for the DI surface and the test surface. BUT: the *consumer-facing path* from "I want MessagePack" to "decorator works in production" requires the consumer to (a) write their own decorator type, (b) register it via `RegisterDecorator(typeof(MyMessagePackDecorator<,>))`, (c) ensure DI resolves it, AND (d) NOT call `AddJsonQueryLogging()`. The ADR's "~10-line custom decorator" claim is optimistic — a working custom decorator must mimic the cached `static readonly Logger` pattern, must handle the fallback-exception bag-key, must duplicate the start/complete log templates, and must implement BOTH sync and async variants. The "escape hatch" is closer to ~40-60 lines per decorator pair, plus DI registration. The ADR overstates how cheap this is.

**Evidence**: ADR Forces line 42 ("*a custom decorator and skips `AddJsonQueryLogging()`*"); Alternatives line 280 ("*a custom decorator is ~10 lines*"); the existing `QueryLoggingDecorator.cs` is ~54 lines, `QueryLoggingDecoratorAsync.cs` is ~60 lines, both required for a sync+async pair.

**Recommendation**: Change "~10 lines" to a realistic count, or qualify with "*~10 lines for a sync-only quick-and-dirty case; ~50+ lines for a production-quality sync+async pair*". This is the load-bearing dismissal for Alternative 1 — it should be honest about cost.

---

### 9. Alternative 2's "Services not available on builder surface" argument is partial (Score: 60)

Alternative 2 ("DI-registered `JsonSerializerOptions` singleton") is dismissed because "*`IDarkerHandlerBuilder.Services` is the DI hook; the builder-extension path (`JsonQueryLogging(IBuildTheQueryProcessor, …)`) doesn't have a `Services` collection*". This is true at the *surface* level — `IBuildTheQueryProcessor` has only `Build()` — but the ADR uses this as a knockdown argument when the existing precedent (`Policies/QueryProcessorBuilderExtensions.cs`) already does asymmetric handling: the builder path has its own state (`QueryProcessorBuilder.PolicyRegistry`) that the canonical generic method writes to, and the DI path uses `Services` collection. So the architecture *already accepts* asymmetric configuration per surface. The "Services not available on builder surface" force is real but it's exactly the kind of asymmetry the codebase already lives with — not a knockdown.

The stronger reason to reject Alternative 2 is the second point in the ADR's "Why not chosen" — `SimpleHandlerDecoratorFactory` cases have no `IServiceProvider`. That is genuinely architectural. The "Services collection" point is weaker than presented.

**Evidence**: ADR Alternatives 2 line 291; the existing precedent that policies-on-builder writes to `QueryProcessorBuilder.PolicyRegistry` (`Policies/QueryProcessorBuilderExtensions.cs:19`) while policies-on-DI writes to `IServiceCollection` is exactly the asymmetric pattern the ADR's force argues against.

**Recommendation**: Demote the "Services collection asymmetry" argument and lead with the `SimpleHandlerDecoratorFactory`-has-no-IServiceProvider argument, which is the actual architectural reason. The current ordering implies the asymmetry is fatal when the codebase already accepts that asymmetry elsewhere.

---

### 10. Forward-versioning of the mutable global is unaddressed (Score: 55)

The ADR pins `QueryLoggingJsonOptions.Options` defaults at V5 (`ReferenceHandler.IgnoreCycles`). It does not address what happens at V6 if maintainers want to change the default — e.g. add a `JsonConverter`, raise `MaxDepth`, or set `PropertyNamingPolicy`. Because the global is process-scope and consumers can use direct assignment that bypasses the defaults, ANY future change to the class-init defaults is a **silent behaviour change** for callback-path consumers and a **no-op** for direct-assignment consumers. Neither group gets a compile-time signal. The ADR's Constraints and "Negative" consequences talk only about V5 migration; the architectural surface remains for V6+.

This isn't a blocker for V5 but it's a documented gap: the ADR claims long-term Brighter parity (forces line 28) but doesn't address the lifecycle of the parity surface.

**Evidence**: ADR lines 60–66 (Constraints) discuss V5 migration only; no mention of default-evolution semantics across versions; ADR Risk-mitigation table does not list "future default changes" as a tracked risk.

**Recommendation**: Add a one-line bullet under "Risks and Mitigations" or under "Negative" consequences acknowledging that future changes to class-init defaults are silent behaviour changes for callback-path consumers; resolution mechanism = release notes + semver discipline. Or document an explicit "defaults are part of the public API surface and protected by semver" rule.

---

### 11. `Logger` field cited as "line ~13" is correct for sync but off for async (Score: 35)

The verification prompt asks about a "cached `static readonly Logger` at line ~13". `QueryLoggingDecorator.cs:13` is correct. `QueryLoggingDecoratorAsync.cs:15` is the equivalent — the ADR's wording in Architecture Overview / Key Components doesn't cite specific line numbers (only the requirements do), so this is at most a minor cite-precision nit. Mention here just for completeness — not material on its own.

**Evidence**: `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecorator.cs:13`, `src/Paramore.Darker/Logging/Handlers/QueryLoggingDecoratorAsync.cs:15`.

**Recommendation**: No change required.

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | 0 |
| 70-89 (High) | 3 |
| 50-69 (Medium) | 7 |
| 0-49 (Low) | 1 |

**Total findings**: 11
**Findings at or above threshold (60)**: 9
