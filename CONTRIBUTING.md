# Contributing

## How to Use This File

Use this file to follow our coding guidelines when contributing to Darker. Darker is the query-side
counterpart of [Brighter](https://github.com/BrighterCommand/Brighter), and the two projects share
their conventions and workflow. The same guidance is restated for coding agents in
[`.agent_instructions/`](.agent_instructions/); if you work with an agent, also read
[AGENTIC_CODING.md](AGENTIC_CODING.md).

## Table of Contents

- [How to Use This File](#how-to-use-this-file)
- [First Time Contributing?](#first-time-contributing)
- [Architecture Decision Records](#architecture-decision-records)
- [Code Style](#code-style)
- [Testing](#testing)
  - [TDD Style](#tdd-style)
  - [Test Doubles](#test-doubles)
- [Documentation](#documentation)
  - [Licensing](#licensing)
- [Dependency Management](#dependency-management)
- [Making Changes](#making-changes)
  - [Build & Test](#build--test)
  - [Commit Messages](#commit-messages)
  - [Release Notes](#release-notes)
  - [Repository Branching Strategy](#repository-branching-strategy)
  - [Submitting Changes](#submitting-changes)
  - [Contributor License Agreement](#contributor-license-agreement)
- [Support for Agentic Coding](#support-for-agentic-coding)
- [Project Structure](#project-structure)

---

## First Time Contributing?

Welcome! Here's how to get started:

### Quick Setup
1. Fork and clone the repository
2. Ensure you have the .NET 8 and .NET 9 SDKs installed: `dotnet --list-sdks`
3. Build the solution: `dotnet build Darker.Filter.slnf`
4. Run the tests (no external dependencies needed): `dotnet test Darker.Filter.slnf`

`Darker.Filter.slnf` excludes the MAUI sample app, which needs extra workloads. Use `Darker.slnx`
only if you are working on that sample.

### Your First Contribution
1. Look through the [open issues](https://github.com/BrighterCommand/Darker/issues) for one you would like to pick up
2. Comment on the issue to let others know you're working on it
3. Read the relevant sections below:
   - [Code Style](#code-style) - Understand our conventions
   - [Testing](#testing) - Learn our TDD approach
   - [Documentation](#documentation) - XML doc requirements
4. Make your changes following our guidelines
5. Submit a PR targeting the `master` branch

### Key Guidelines at a Glance
- ✅ Use TDD where possible (write tests first)
- ✅ Prefer real, Simple or InMemory implementations over mocks in tests
- ✅ Add XML documentation to all public APIs
- ✅ Constants use `ALL_CAPS` naming convention
- ✅ Include license header: `#region Licence` (British spelling)
- ✅ Use Conventional Commits for commit messages
- ✅ Keep the sync and async pipelines in step — a change to one usually needs its twin

---

## Architecture Decision Records

- If adding a new capability to Darker, write an Architecture Decision Record (ADR) in `docs/adr/`.
  - The ADR should focus on the *why* of your decision, over implementation details, which can be
    better found in the code.
  - Use the ADR to agree what you want to do, before you do it.
  - Use the ADR to signal to others, including future maintainers, why Darker is built the way it is.
- Follow the skeleton, readability rules and diagram rules in
  [documentation.md](.agent_instructions/documentation.md) § *Architecture Decision Records*.
  [ADR 0001](docs/adr/0001-record-architecture-decisions.md) is the minimal template.
- Every ADR starts with a short YAML frontmatter block (`id`, `title`, `status`, `author`,
  `created`, `summary`, `tags`), described in [adr_frontmatter.md](.agent_instructions/adr_frontmatter.md).
  [`docs/adr/index.md`](docs/adr/index.md) is generated from that frontmatter, so read it to find
  related decisions, but never edit it by hand.

You can create the ADR as the first step on a new branch or fork. Your first commit then contains
the ADR describing the change, which lets you open a draft PR so others can check their
understanding of the change and give feedback early, before expectations drift apart. If you
change the design as you learn, add another ADR that supersedes the old one.

## Code Style

- Follow .NET C# conventions
  - Use [Microsoft's C# naming conventions for identifiers](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names)
  - For a const, use an All Caps naming convention, with underscores between words i.e.
    `MAX_RETRY_COUNT`. This replaces rules in the Microsoft C# naming convention.
  - Follow [Microsoft's C# coding conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
  - DO NOT use Microsoft's Framework Design Guidelines. They are not idiomatic and outdated.
- Prefer expression-bodied members for simple properties and methods.
- Prefer primary constructors where possible, especially for simple classes and records.
- Use readonly for fields that do not change after construction.
- Enable nullable reference types (`<Nullable>enable</Nullable>`), and make types nullable to
  indicate optionality.
- You may use marker interfaces. We find them useful as a base type for async and sync interfaces.
- We support both sync and async pipelines.
  - Suffix async methods with `Async`.
  - Handlers, decorators and their attributes come in separate sync and async types
    (see [ADR 0008](docs/adr/0008-split-handler-interfaces.md)). When you change one path, change
    its twin, or say why not.
- Use assemblies to provide modularity, divided by optionality.
  - Required behavior lives in `Paramore.Darker`.
  - Optional behavior, such as DI integration, diagnostics, validation providers or caching, lives in
    its own assembly, so users only take a dependency on the NuGet packages they need.
- Default to a class per source file, unless one class clearly exists as the details of another.
- Use Responsibility-Driven Design — see [design_principles.md](.agent_instructions/design_principles.md).
  - Specify object behavior before object structure.
  - Think of responsibilities for "knowing", "doing" and "deciding", and allocate them to roles.
  - Distribute behavior: make objects smart, not just holders of data.
  - Preserve flexibility: design objects so interior details can readily change.
- Avoid primitive obsession. Where a primitive could be replaced with a more expressive type, use a
  class, struct or record.
- Follow Beck's "Tidy First" approach:
  - Separate structural changes (renaming, extracting methods, moving code) from behavioral changes.
  - Never mix the two in the same commit, and make structural changes first.
  - Run the tests before and after a structural change to show behavior has not changed.
- Not all of our code follows these conventions yet. Follow the boy scout rule and fix what you
  touch.

The full guidance is in [code_style.md](.agent_instructions/code_style.md).

## Testing

- Use TDD where possible.
- Write developer tests using xUnit, with Shouldly for assertions.
- Name test methods `When_[condition]_should_[expected_behavior]`, and name the file after the test
  method. Name test classes `[Behavior]Tests`, never with `When_`.
- Prefer one test case per file.
- Ensure all new features and bug fixes include appropriate test coverage.

### TDD Style

- We write developer tests: a failing test implicates the most recent edit. We do not use mocks to
  isolate the system under test.
  - You might want to watch [this video](http://vimeo.com/68375232) to understand our preferred
    testing approach, or review
    [TDD Revisited](https://github.com/iancooper/Presentations/blob/master/Kent%20Beck%20Style%20TDD%20-%20Seven%20Years%20After.pdf).
- Where possible, we are test first: Red, Green, Refactor.
  - Only write the code a test needs to pass; do not write speculative code.
- Tests confirm behavior, not implementation details. It should be possible to refactor without
  breaking tests.
  - Use Arrange/Act/Assert, made explicit with comments.
  - Use the Evident Data pattern: highlight the state that affects the outcome.
- The trigger for a new test is a new behavior, not a new method.
- Only test exports from an assembly: public methods on public classes.
  - Do not make something public just to test it, and never use `InternalsVisibleTo`. If no existing
    export reaches the behavior, read *Narrow and deep* in
    [testing.md](.agent_instructions/testing.md) before widening the surface.

### Test Doubles

Prefer, in this order:

1. **Real instances** — for example `QueryHandlerRegistry` or `InMemoryQueryContextFactory`.
2. **Simple implementations** — delegate-based, such as `SimpleHandlerFactory` and
   `SimpleHandlerDecoratorFactory`.
3. **InMemory implementations** — such as `InMemoryDecoratorRegistry`.
4. **Mocks** — a last resort, only for I/O boundaries or interactions that cannot be observed through
   behavior.

Put test-specific handlers, queries and decorators in the test project's `TestDoubles/` directory
(for example `test/Paramore.Darker.Core.Tests/TestDoubles/`), one class per file. The full guidance
is in [testing.md](.agent_instructions/testing.md).

## Documentation

- Add or update XML documentation comments (`///`) for all exports from assemblies: public and
  protected members of public types. Do not document internal or private members.
- Write for a developer reading IntelliSense: helpful, but short enough that they will read it.
  - `<summary>` — what the type or member is for. Use `<paramref>` to refer to parameters.
  - `<param>` — what the parameter is for, its effect, and any default if it is optional.
  - `<returns>`, `<typeparam>`, `<exception>` and `<value>` as appropriate, with `<see cref=""/>`
    for types.
  - `<remarks>` — implementation notes or design decisions a maintainer would want to know.
- Prefer intention-revealing names to inline comments. In tests you may use `//Arrange`, `//Act`,
  `//Assert`.
- Update documentation comments when APIs change.
- Document new features in the [Docs repository](https://github.com/BrighterCommand/Docs) of the
  BrighterCommand organization.

The full guidance, including the ADR contract and writing tone for design documents, is in
[documentation.md](.agent_instructions/documentation.md).

### Licensing

- We add an MIT license comment to the top of every source file, before any `using` statements.
- Put it in a `#region Licence` block (British spelling, no space).
- Add your name and the year for a new file. An agent should use the name of the contributor
  directing it.

```csharp
#region Licence

/* The MIT License (MIT)
Copyright © [Year] [Your Name] [Your Contact Email]

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. */

#endregion
```

## Dependency Management

- We use Central Package Management: package versions live in `Directory.Packages.props`, and
  project files reference packages without a version.

```xml
<!-- Directory.Packages.props -->
<ItemGroup>
  <PackageVersion Include="Polly" Version="8.7.0" />
</ItemGroup>

<!-- A project file -->
<ItemGroup>
  <PackageReference Include="Polly" />
</ItemGroup>
```

- Align all `Microsoft.Extensions.*` and `System.*` package versions.
- Avoid mixing preview and stable package versions.
- Keep the core `Paramore.Darker` package's dependencies to a minimum; an optional dependency
  belongs in an optional assembly.

## Making Changes

- Make sure you have the latest version of `master`.
- Raise an issue in the GitHub issue tracker, after checking whether someone has already raised it.
- If you want to add a feature, as opposed to fixing something that is broken, still raise an issue.
  It lets us give specific advice and check the feature does not conflict with work in progress.
- Comment on an issue when you pick it up, so everyone else knows.
- If you have a defect, include:
  - Steps to reproduce the issue
  - A failing test if possible
  - The stack trace for any errors you encountered

### Build & Test

```bash
# Build (excludes the MAUI sample)
dotnet build Darker.Filter.slnf

# Run all tests — none need external services
dotnet test Darker.Filter.slnf

# Run one test project
dotnet test test/Paramore.Darker.Core.Tests

# Run a single test
dotnet test test/Paramore.Darker.Core.Tests --filter "FullyQualifiedName~When_query_handler_not_registered"
```

Ensure all tests pass before submitting a PR. CI builds and tests against .NET 8 and .NET 9.

### Commit Messages

- Try to write a [good commit message](http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html).
- Use the imperative mood (e.g., "Add support for X", "Fix bug in Y").
- Reference issues or PRs where relevant.
- Use [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/#specification)
  (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`).

### Release Notes

If your change breaks an existing behavior or public interface, add a section for it under
`## Master` in [release_notes.md](release_notes.md), saying what breaks and how to migrate. The
`/spec:write_release_notes` command drafts this section from the ADRs for a spec.

### Repository Branching Strategy

| Branch | Description |
| --- | --- |
| `master` | The tip of active development. Anything in `master` should ship in the next release, and must compile and pass tests. |
| Release branches | The code for an actively supported release, created when `master` needs breaking changes the current release cannot take. |
| Other branches | Work that is not ready for `master` (for example, it would break CI) or is experimental. |

**When submitting a pull request:** target `master` unless you're fixing a bug in a specific release
branch.

### Submitting Changes

- Fork the project and clone your fork
- Create a branch for your work
- Push to your fork
- Submit a pull request against `master`
- Respond to review feedback; we will get to it as soon as we can

### Contributor License Agreement

To safeguard the project, we ask you to sign a Contributor License Agreement. You keep your
copyright, but grant the project the right to use your contribution in perpetuity, so that the
project is not at risk from any one contributor withdrawing their grant of license.

The agreement is in [CLA.txt](CLA.txt), and signing works through GitHub:
<a href="https://www.clahub.com/agreements/iancooper/Paramore">sign the Contributor License Agreement</a>.

## Support for Agentic Coding

We welcome code authored with a coding agent. However, you are responsible for the code you submit:
review it, understand it, and make sure the agent followed these guidelines.

Our agent support is focused on Claude Code, though other agents can use the same instructions in
`.agent_instructions/`. See **[AGENTIC_CODING.md](AGENTIC_CODING.md)** for:

- the instruction files and slash commands we provide;
- the `/spec` workflow for features: requirements, ADRs, adversarial review, tasks, TDD
  implementation, and a code review of the branch;
- how to choose a review gear with `/spec:gear`, and whether to work a task at a time with
  `/spec:implement` or run a loop with `/spec:ralph-implement`;
- the `/bugfix` workflow, which proves a bug's root cause before fixing it.

### Contributor Code of Conduct

Please note that this project is released with a Contributor Code of Conduct. By participating in
this project you agree to abide by its terms. The code of conduct is from the
[Contributor Covenant](http://contributor-covenant.org/).

## Project Structure

- `src/` — the libraries, one assembly per responsibility:
  - `Paramore.Darker` — the core: `QueryProcessor`, the pipeline builder, registries and factories,
    the built-in decorators (query logging, retry and fallback policies, Polly resilience
    pipelines), streaming queries, and observability.
  - `Paramore.Darker.Extensions.DependencyInjection` — integration with
    `Microsoft.Extensions.DependencyInjection`, including handler assembly scanning.
  - `Paramore.Darker.Extensions.Diagnostics` — wires Darker's tracing and metrics into OpenTelemetry.
  - `Paramore.Darker.Caching` — the query result caching decorator.
  - `Paramore.Darker.Validation` — the provider-agnostic validation decorator, with
    `Paramore.Darker.Validation.DataAnnotations` and `Paramore.Darker.Validation.FluentValidation`
    providers.
  - `Paramore.Darker.Testing` — helpers for testing code that uses Darker.
- `test/` — test projects, partitioned to match the libraries (`Paramore.Darker.Core.Tests`,
  `Paramore.Darker.Extensions.Tests`, `Paramore.Darker.Caching.Tests`, the validation test projects,
  and so on), plus `Paramore.Darker.Tests.AOT` for trimming/AOT compatibility,
  `Paramore.Darker.Benchmarks`, and the shared `Paramore.Test.Helpers`.
- `samples/` — `SampleMinimalApi` (an ASP.NET Core minimal API) and `SampleMauiTestApp`.
- `docs/adr/` — Architecture Decision Records, with a generated `index.md`.
- `specs/` — specifications produced by the `/spec` workflow.
