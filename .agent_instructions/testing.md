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
- This is NOT optional - the approval gate is MANDATORY when working with Claude Code

This ensures the mandatory approval step is never skipped and tests are reviewed before implementation.

- We write developer tests
  - Failure of a test case implicates the most recent edit.
  - Do not use mocks to isolate the System Under Test (SUT).
    - We prefer developer tests that implicate the most recent edit, not isolation of classes.
- Where possible, we are test first
  - Red: Write a failing test
  - **APPROVAL**: Get approval for the test before implementing
  - Green: Make the test pass, commit any sins necessary to move fast
  - Refactor: Improve the design of the code.
- **Approval Workflow** (MANDATORY - NOT OPTIONAL):
  - When working on a feature, ALWAYS use `/test-first <behavior>` - do not write tests manually
  - The skill will write the test and ASK FOR APPROVAL before proceeding
  - The user will review the test in their IDE
  - DO NOT run tests or start implementation without explicit user approval
  - After approval, implement the minimum code to make the test pass
  - The approval step is MANDATORY when working with Claude Code - you cannot bypass it
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

## No InternalsVisibleTo

- **NEVER use `InternalsVisibleTo` to expose internal classes for testing.**
- Internal classes should NOT be driven by unit tests directly.
- Internal classes should emerge as implementation details through refactoring:
  1. First, implement behavior in the public class (keep it simple)
  2. As complexity grows, extract internal helper classes through refactoring
  3. Tests always go through the public interface - internal classes are covered by those tests
- If you need to inject a dependency for testing (e.g., randomness, I/O), make the interface **public** so it can be injected through the public API.
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
