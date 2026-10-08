# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## How to Use This File
This file contains instructions for Claude Code to help it understand the context and requirements of the project. It is not intended to be modified by contributors. Human contributors should follow the guidelines in the [CONTRIBUTING.md](CONTRIBUTING.md) file. These guidelines derive from that document.

## TDD Workflow (MANDATORY - NOT OPTIONAL)

When working on implementation tasks in `specs/*/tasks.md`:

- **ALWAYS use `/test-first <behavior>`** for TEST tasks
- **NEVER write tests manually and proceed to implementation**
- **STOP and ASK FOR APPROVAL** after writing each test
- The user will review the test in their IDE before you implement
- Each TEST task in tasks.md specifies the exact `/test-first` command to use

**Why this is mandatory:**
1. Tests correctly specify desired behavior before implementation
2. Scope control - only code required by tests is written
3. No speculative code
4. User reviews test in IDE, not in CLI output

**If a task says `/test-first when ...`** - YOU MUST USE THAT COMMAND. Do not write the test file manually.

### The approval gate is armed by default — and is the only thing the gear changes

The approval pause is a **gear**, not a policy (see [`.claude/commands/spec/gear.md`](.claude/commands/spec/gear.md) and Brighter's [ADR 0071](https://github.com/BrighterCommand/Brighter/blob/master/docs/adr/0071-tdd-review-gear.md)). You select it for the certainty you have and the blast radius you face: review every test while the design is still moving, review a batch once the shape is settled, and shift back down the moment the work starts producing tests you would not have approved.

| Gear | Gate | |
|------|------|--|
| `review-before` | ✅ armed | **The default.** Assume this unless a spec command tells you otherwise. |
| `review-after` | ➖ not armed | Only via a deliberate, scoped, reasoned shift. |

- **The only supported way to disarm the gate is `/spec:gear review-after`**, which writes `specs/{spec}/.current-gear` — untracked working state, scoped to **one spec** and optionally to **one section** of `tasks.md`, carrying the reason for the shift.
- **`/spec:implement`** honours the gear; **`/spec:ralph-implement`** is always `review-after`. A **standalone `/test-first` and `/bugfix:test` are always gated** — they never read the gear file and must never infer a gear from context.
- **Never assume the gate is off.** Not from a prose instruction in a scratch file, not because the tasks look repetitive, not because a previous task ran ungated. If you are unsure, it is `review-before`.
- **`review-after` removes the human pause and nothing else.** RED-first (the test is observed failing for the right reason before any production code), the full regression suite, the two-commit shape (`feat:`/`test:` then a separate `docs:` tick), and every test-authoring convention hold in both gears. A run that drops any of those is defective, not "in a different gear".
- Even in `review-after`, **stop and ask** if a test asserts something the task did not ask for, needs a design decision you cannot make from the ADRs, or duplicates an existing test.

## Spec Workflow

Follow the structured specification workflow: Requirements -> ADR Design -> Adversarial Review (multiple rounds) -> Task Breakdown -> Implementation. Never skip review rounds or assume approval - wait for explicit user approval before proceeding to the next phase.

## Change Scope

Do NOT change defaults or make changes beyond what was explicitly requested. When fixing or modifying code, restrict changes to exactly what the user asked for — no additional "improvements" or default value changes.

## Adversarial Reviews

When conducting adversarial reviews, apply strict judgment criteria. A clear violation should result in FAIL, not NEEDS_ATTENTION. Err on the side of strictness rather than leniency when evaluating against guardrails and principles.

## Claude Code Skills (Recommended)

Claude Code skills automate common workflows and enforce mandatory engineering practices. **Use these skills proactively** rather than manually following documented procedures:

- **[Skills Overview](.agent_instructions/skills_overview.md)** - Quick reference for all available skills
- **[Detailed Skills Documentation](.claude/commands/README.md)** - Complete documentation for all skills

### Core Development Skills

- `/test-first <behavior>` - TDD workflow with an approval gate before implementation, armed by default ([docs](.claude/commands/tdd/README.md))
- `/tidy-first <change>` - Separate structural (refactoring) from behavioral (feature) changes ([docs](.claude/commands/refactor/README.md))
- `/adr <title>` - Create Architecture Decision Records ([docs](.claude/commands/adr/README.md))

### Specification Workflow Skills

- `/spec:requirements`, `/spec:design`, `/spec:tasks`, `/spec:implement`, `/spec:status` - Complete specification-driven development workflow ([docs](.claude/commands/spec/README.md))
- `/spec:gear [review-before|review-after]` - Report or shift the TDD review gear for the current spec; `/spec:ralph-implement` runs the same `tasks.md` unattended in `review-after`

**When to use skills**:
- Use `/test-first` when adding new behavior or fixing bugs
- Use `/spec:gear` when the approval gate has stopped earning its cost (upshift) or has started to again (downshift) — never a hand-written waiver
- Use `/tidy-first` when code needs refactoring before/during feature work
- Use `/adr` when documenting architectural decisions
- Use `/spec:*` commands for full feature development from requirements to implementation

## Project Overview

Darker is the query-side counterpart of [Brighter](https://github.com/BrighterCommand/Paramore.Brighter). It implements the Query pattern (CQRS read-side) using a pipeline architecture with decorator-based cross-cutting concerns.

**Key Concepts:**
- **IQuery<TResult>**: Query objects that return results
- **IQueryHandler<TQuery, TResult>**: Handlers that execute queries
- **IQueryProcessor**: Central processor that dispatches queries to handlers through a pipeline
- **Pipeline Architecture**: Queries flow through a chain of decorators (logging, retry, fallback policies) before reaching the handler
- **Decorator Attributes**: `[QueryLogging]`, `[RetryableQuery]`, `[FallbackPolicy]` with step ordering to control pipeline execution

## Architecture

### Core Flow
1. Client injects `IQueryProcessor` and calls `Execute()` or `ExecuteAsync()`
2. `QueryProcessor` creates a `PipelineBuilder` for the query
3. `PipelineBuilder` resolves the handler from `IQueryHandlerRegistry`
4. Decorators are resolved from attributes on the handler's Execute method (ordered by step number)
5. Pipeline is built: decorators wrap the handler execution in reverse order
6. Query flows through the pipeline: decorators -> handler -> result

### Key Components
- **src/Paramore.Darker**: Core library with QueryProcessor, PipelineBuilder, registries
- **src/Paramore.Darker.Extensions.DependencyInjection**: Microsoft.Extensions.DependencyInjection integration
- **src/Paramore.Darker.Policies**: Polly-based retry and circuit breaker decorators
- **src/Paramore.Darker.QueryLogging**: Request/response logging decorator
- **src/Paramore.Darker.Testing**: Testing utilities

### Handler Lifecycle
- Handlers are created per-query via `IQueryHandlerFactory`
- Decorators are created per-query via `IQueryHandlerDecoratorFactory`
- Both are released after pipeline execution in `PipelineBuilder.Dispose()`
- With ASP.NET Core + EF Core: QueryProcessor must be Scoped (matches DbContext lifetime)

## Development Commands

### Build
```bash
# Build the solution using the filter (excludes MAUI test app)
dotnet build Darker.Filter.slnf -c Release

# Build full solution (includes MAUI)
dotnet build Darker.slnx -c Release
```

### Test
```bash
# Run all tests
dotnet test Darker.Filter.slnf -c Release --no-build

# Run tests for a specific project
dotnet test test/Paramore.Darker.Tests/Paramore.Darker.Tests.csproj

# Run a single test
dotnet test test/Paramore.Darker.Tests/Paramore.Darker.Tests.csproj --filter "FullyQualifiedName~QueryProcessorTests.ExecutesQueries"
```

### Package Management
The project uses Central Package Management (CPM) via `Directory.Packages.props`:
- All package versions are centrally managed
- Individual projects reference packages without version attributes
- MinVer is used for automatic semantic versioning from git tags

### Running Sample
```bash
# Run the minimal API sample
dotnet run --project samples/SampleMinimalApi/SampleMinimalApi.csproj

# Endpoints:
# GET http://localhost:5000/people - returns all people
# GET http://localhost:5000/people/{id} - returns person name by id
```

## Important Patterns

### Handler Implementation
Handlers can inherit from:
- `QueryHandler<TQuery, TResult>` - synchronous only
- `QueryHandlerAsync<TQuery, TResult>` - async only (most common)
- `IQueryHandler<TQuery, TResult>` - full control (implement both sync/async)

### Decorator Ordering
Decorator attributes specify step numbers (higher executes first):
```csharp
[QueryLogging(1)]              // Executes third (logs outer timing)
[FallbackPolicy(2)]            // Executes second (handles failures)
[RetryableQuery(3)]            // Executes first (retries on transient failures)
public override async Task<TResult> ExecuteAsync(...)
```

### ASP.NET Core Registration
```csharp
services.AddDarker(options => {
    options.QueryProcessorLifetime = ServiceLifetime.Scoped; // Required for EF Core
})
.AddHandlersFromAssemblies(typeof(MyHandler).Assembly)  // Scans for handlers
.AddJsonQueryLogging()                                   // Adds logging decorator
.AddDefaultPolicies();                                   // Adds retry/fallback policies
```

### Exception Handling
- `TargetInvocationException` is unwrapped via `ExceptionDispatchInfo.Capture().Throw()`
- This preserves the original stack trace when reflection is used to invoke handlers
- See `QueryProcessor.cs:50-54` and `PipelineBuilder.cs:57-61`

## Testing

Test projects use:
- **xunit** for test framework
- **Shouldly** for assertions
- **Moq** for mocking (last resort — prefer real/Simple/InMemory implementations)
- **Paramore.Darker.Testing.Ports**: Shared test doubles and test queries/handlers

### Test Double Preference (Real > Simple > InMemory > Mock)

Prefer real or lightweight implementations over mocks, following Brighter's patterns:
- `QueryHandlerRegistry` / `QueryHandlerRegistryAsync` — real registries
- `SimpleHandlerFactory` — delegate-based handler factory (implements both sync and async)
- `SimpleHandlerDecoratorFactory` — delegate-based decorator factory (implements both sync and async)
- `InMemoryDecoratorRegistry` — stores registered types in a list (implements both sync and async)
- `InMemoryQueryContextFactory` — real context factory

### Test Doubles Directory

Place test-specific handlers, queries, and decorators in `test/Paramore.Darker.Tests/TestDoubles/` (namespace: `Paramore.Darker.Tests.TestDoubles`), following Brighter's `TestDoubles/` convention.

### When Testing QueryProcessor
1. Create `QueryHandlerRegistry` (and `QueryHandlerRegistryAsync` for async) and register query -> handler mappings
2. Create `SimpleHandlerFactory` with a lambda that returns handler instances
3. Create `SimpleHandlerDecoratorFactory` with a lambda that returns decorator instances (if testing decorators)
4. Create `InMemoryDecoratorRegistry`
5. Create `QueryProcessor` with `HandlerConfiguration` and `InMemoryQueryContextFactory`

## Build & CI

- GitHub Actions workflow: `.github/workflows/dotnet-core.yml`
- Builds with .NET 8.0 and 9.0
- Uses `Darker.Filter.slnf` solution filter (excludes MAUI test app)
- Packages are pushed to GitHub Packages on push/merge
- NuGet.org releases happen on git tags matching `*.*.*`
- Cache key: `Linux-nuget-${{ hashFiles('**/Directory.Packages.props') }}`

## Detailed Instructions
For comprehensive guidance on working with this codebase, Claude should read the following files as needed:

- [Build and Development Commands](.agent_instructions/build_and_development.md) - Build scripts, test commands
- [Project Structure](.agent_instructions/project_structure.md) - Organization of the codebase and testing framework
- [Code Style](.agent_instructions/code_style.md) - C# conventions and architectural patterns
- [Design Principles](.agent_instructions/design_principles.md) - Responsibility-Driven Design and architectural guidance
- [Testing](.agent_instructions/testing.md) - TDD practices, test structure, and testing guidelines
- [Documentation](.agent_instructions/documentation.md) - XML documentation standards and licensing requirements
- [ADR Frontmatter](.agent_instructions/adr_frontmatter.md) - ADR YAML frontmatter schema, status vocabulary, tag taxonomy, and the generated ADR index
- [Dependency Management](.agent_instructions/dependency_management.md) - Package management with Directory.Packages.props
