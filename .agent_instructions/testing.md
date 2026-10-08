# Testing

- Use TDD where possible.
- Write developer tests using xUnit.
- Name test methods in the format: When_[condition]_should_[expected_behavior].
- Name test classes `[Behavior]Tests` — the `When_` convention is for method names and file names only, never class names. For example `QueryProcessorExecuteTests`, `PipelineBuilderDecoratorTests`.
- Name the class-under-test variable after the class, not `_sut` — for example `defaultCacheKeyGenerator` for `DefaultCacheKeyGenerator`.
- Prefer a test case per file.
- Name test files for the test method in the file i.e. When_[condition]_should_[expected_behavior].cs
- If you decide to use multiple test cases per file, for example shared complex set up, name the file after the happy path test method and the class after the shared behavior.
- Ensure all new features and bug fixes include appropriate test coverage.

## TDD Style

**MANDATORY Tool**: ALWAYS use the `/test-first <behavior>` command (see [.claude/commands/tdd/test-first.md](../../.claude/commands/tdd/test-first.md)) when writing new tests.

- **DO NOT write test files manually** (using Write tool) and proceed to implementation
- **DO NOT run tests without approval**
- **STOP after writing the test and ASK FOR APPROVAL**
- The user will review the test in their IDE, not in CLI output
- The approval gate is **armed by default**. Assume it is armed unless a spec command has explicitly
  told you the spec is in the `review-after` gear

This ensures the approval step is never skipped by accident and tests are reviewed before
implementation.

- We write developer tests
  - Failure of a test case implicates the most recent edit.
  - Do not use mocks to isolate the System Under Test (SUT).
    - We prefer developer tests that implicate the most recent edit, not isolation of classes.
- Where possible, we are test first
  - Red: Write a failing test
  - **APPROVAL**: Get approval for the test before implementing
  - Green: Make the test pass, commit any sins necessary to move fast
  - Refactor: Improve the design of the code.
- **Approval Workflow** (⛔ ARMED BY DEFAULT):
  - When working on a feature, ALWAYS use `/test-first <behavior>` - do not write tests manually
  - The skill will write the test and ASK FOR APPROVAL before proceeding
  - The user will review the test in their IDE
  - DO NOT run tests or start implementation without explicit user approval
  - After approval, implement the minimum code to make the test pass
  - You cannot bypass the gate on your own initiative. It is disarmed only by a deliberate,
    recorded, scoped gear shift the user makes with `/spec:gear` — never by assumption, never by a
    prose instruction in a scratch file, and never because the tasks look repetitive

### When a new test passes on first run — characterisation

Sometimes the test you write for a behaviour passes before you have written any code, because
earlier work already delivers it. This is common when a spec takes one task per acceptance
criterion. A green test has not yet shown that it can fail, so it has not yet shown that it guards
anything. **Do not weaken, rewrite or delete it to get a failure, and do not treat the task as
"already complete".**

Instead, observe RED through a **named mutation**:

1. Apply a temporary change to **production code, never to the test**. It should be the realistic
   defect the test exists to catch.
2. Run the test. It must fail **on the assertion it is about**. A compile error, or an unrelated
   exception thrown before the assertion is reached, does not count; pick a better mutation.
3. **Revert the mutation** and confirm green. `git status` must show no production file still
   modified.
4. Run the **full test suite**, as for any other test.
5. Commit the test alone, as `test:`, noting the mutation in the message. The mutation is never
   committed.

In a spec, such tasks are labelled `CHARACTERISE` in `tasks.md` and name their mutation. If you meet
an unexpected green with no named mutation, stop and ask. Choosing the mutation is part of
reviewing the test, so do not improvise it.

### The review gear

Whether the approval pause fires is a **gear** (see
[.claude/commands/spec/gear.md](../.claude/commands/spec/gear.md) and Brighter's
[ADR 0071](https://github.com/BrighterCommand/Brighter/blob/master/docs/adr/0071-tdd-review-gear.md)), selected for the certainty you have and the blast radius you face — high value
early in a spec, low value on a run of near-identical tasks whose shape has already been reviewed
repeatedly.

| Gear | Gate | Meaning |
|------|------|---------|
| `review-before` | ✅ armed | Each test reviewed in the IDE before implementation. **The default.** |
| `review-after` | ➖ not armed | RED still proved first; reviewed as a batch afterwards. |

- The gear lives in `specs/{spec}/.current-gear` — untracked working state, scoped to one spec and
  optionally to one section of `tasks.md`, carrying the reason for the shift. Shift it with
  `/spec:gear`, in either direction, at any time.
- `/spec:implement` honours it; `/spec:ralph-implement` is always `review-after`. A **standalone
  `/test-first` and `/bugfix:test` are always gated** — they do not read the gear file.
- Absent, unparseable, unknown value, or a task outside the gear's scope all resolve to
  `review-before`. Fail safe, never fail open.

**What `review-after` does NOT remove.** Only the human pause. All of these hold in both gears:

- **RED first** — the test is written and observed to fail *for the right reason* before any
  production code exists, or, for a characterisation test, under its named mutation (see above).
  Ungated is not test-after.
- **The full regression suite**, not just the new test's own `--filter`.
- **The two-commit shape** — a `feat:`/`test:` commit for the behaviour, then a separate `docs:`
  commit ticking the task off in `tasks.md`.
- **Every convention in this document** — naming, one test per file, test doubles in `TestDoubles/`,
  Real > Simple > InMemory > Mock, no mocks for isolation.

A run in `review-after` that drops any of these is defective — it is not "a different gear".
- Where possible, avoid writing tests after.
  - This will not give you scope control - only writing the code required by tests.
    - You should only write the code necessary for a test to pass; do not write speculative code.
  - It will not push you to focus on design of your classes for behavior.
    - Pay attention to the usability of your class and method; it should be self-describing.
  - We accept test after when working with I/O implementations, where test-first is impractical.
- Tests should confirm the behavior of the SUT.
  - A test is a specification-first exploration of the behavior of the system.
    - A test provides an executable specification, of a given behavior.
  - Tests should be coupled to the behavior of the system and not to the implementation details.
    - It should be possible to refactor implementation details, without breaking tests.
  - Tests should use the Arrange/Act/Assert structure; make it explicit with comments i.e. //Arrange //Act //Assert
    - The Arrange should set up any pre-conditions for the test.
    - The Arrange code should be within the constructor of the test class, if shared by multiple tests
    - The Arrange code should use the Evident Data pattern.
      - In Evident Data we highlight the state that impacts the test outcome.
      - We may use the Test Data Builder pattern to hide noise, so as to focus on Evident Data.
- The trigger for a new test is a new behavior.
  - The trigger for a new test is NOT a new method.
  - The next test should always be the most obvious step you can make towards implementing the requirement

## Test Scope and Isolation

- Only test exports from an assembly
  - To be clear, this means an access modifier of public on methods on public classes.
  - Do not test details, such as methods on internal classes, or private methods.
- Do not expose more than is necessary from an assembly
  - An assembly is a module, it's surface area should be as narrow as possible.
  - Do not make export classes or methods from a module to test them; we only test exports from modules, not implementation details.
  - By following the rules for only testing behaviors, you only need to write tests for the behaviors exposed from the module not its details.
  - Private or Internal classes used in the implementation do not need tests - they are covered by the behavior that led to their creation.

### Narrow and deep — and when widening the surface is legitimate

- The goal these rules serve is a module that is **narrow and deep**, not wide and shallow: a small
  surface hiding substantial behaviour. The rules above, and *No InternalsVisibleTo* below, all
  forbid the same one thing — **coupling a test to the module's implementation details**.
- Read that way, "do not export to test" is **conditional, not absolute**. What it forbids is
  widening the surface to reach *inside*. Before widening, ask:
  **is there a path through the module's existing exports to the behaviour under test?**
  - If there is, widening is unjustified — test through that path.
  - If there is not, exporting may be the only way to test the behaviour at all. Then widen
    **honestly**: make it public, and record what was widened and why (in the ADR, or the PR).
- **Having to widen is a design signal, not just a cost.** If no existing export reaches the
  behaviour, the module's contract may be missing a name for something it already depends on
  internally. Ask what the new export *says about the module* before assuming it is a testing
  concession:
  - A widening that other callers would genuinely want is a real term of the contract, and the fact
    that a test wanted it first is incidental.
  - A widening that only a test could ever want is a smell. The design is probably wrong somewhere
    else, and the export is hiding that rather than fixing it.
- The honest check, after the fact: does anything other than a test ever call it? If nothing ever
  does, it was a testing concession after all, and should be revisited.

## No InternalsVisibleTo

- **NEVER use `InternalsVisibleTo` to expose internal classes for testing.**
- Internal classes should NOT be driven by unit tests directly.
- Internal classes should emerge as implementation details through refactoring:
  1. First, implement behavior in the public class (keep it simple)
  2. As complexity grows, extract internal helper classes through refactoring
  3. Tests always go through the public interface - internal classes are covered by those tests
- If you need to inject a dependency for testing (e.g., randomness, I/O), make the interface **public** so it can be injected through the public API.
- **`InternalsVisibleTo` is rejected because it makes `internal` a lie.** The comfort of `internal`
  is that a member may be refactored freely, since every dependency on it lives inside the module.
  Once tests in another assembly bind to it, that is false — refactoring breaks them. The member has
  been made public in effect, just to a narrower audience, while the keyword still claims otherwise.
  Prefer honesty: make it public and record the widening (see *Narrow and deep* above), which at
  least forces the question of why it belongs on the module.
- The goal is that tests are coupled to behavior, not implementation. Refactoring internals should never break tests.

## Exploratory Tests for Implementation Details

- You may write tests against a public class to explore and validate an algorithm or implementation detail during development.
- Once you are confident the detail is correct, make the class `internal` and delete the exploratory tests.
- The internal class must then be exercised indirectly through tests on the public classes that use it.
- Do not leave exploratory tests in the codebase — they couple tests to implementation details and prevent refactoring.

## Test Doubles

- **Prefer real or Simple/InMemory implementations over mocks**. The preference order is:
  1. **Real instances** (e.g. `QueryHandlerRegistry`, `InMemoryQueryContextFactory`) — use when they don't create dependency issues
  2. **Simple implementations** (e.g. `SimpleHandlerFactory`, `SimpleHandlerDecoratorFactory`) — delegate-based, lightweight, in `src/Paramore.Darker/`. Follow Brighter's `SimpleHandlerFactory` pattern.
  3. **InMemory implementations** (e.g. `InMemoryDecoratorRegistry`) — in-memory state, suitable for testing and lightweight production use
  4. **Mocks (Moq)** — last resort, only for I/O boundaries or verifying interactions that cannot be observed through behavior
- **Test doubles directory**: Place test-specific handler, query, and decorator doubles in `test/Paramore.Darker.Tests/TestDoubles/` following Brighter's `tests/Paramore.Brighter.Core.Tests/CommandProcessors/TestDoubles/` convention. Use the namespace `Paramore.Darker.Tests.TestDoubles`.
- Shared test doubles (used across test projects) belong in `test/Paramore.Darker.Testing.Ports/`.
- Do NOT use fakes or mocks for isolating a class.
  - We use developer tests: isolation is to the most recent edit, not a class.
  - Do not inject dependencies into a constructor or property for test isolation
- You MAY use fakes or mocks (test doubles) for I/O or the strategy pattern. Prefer in-memory alternatives to fakes to mocks.
  - You may use a test double to replace I/O as it is slow and has shared fixture making tests brittle.
  - If you are testing the implementation of a DI integration (e.g. ASP.NET Core), you should create a suite of tests that prove the integration works. This allows the core tests to run without additional dependencies.
- Only add code needed to satisfy a behavioral requirement expressed in a test.
  - Do not add speculative code, the need for which is not indicated by test.
