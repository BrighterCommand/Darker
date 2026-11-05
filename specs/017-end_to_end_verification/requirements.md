# Requirements

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: none. The design is [ADR 0027](../../docs/adr/0027-end-to-end-verification-outside-developer-tests.md) (Accepted), which this document restates as requirements.

## Problem Statement

As a Darker maintainer, I would like V5 to be checked in the way a user uses it, so that a faulty registration order, a missing public registration or a broken sample reaches CI and not a user.

Every Darker test builds its own container and its own doubles. Those tests show that each part works. Three questions about the composed product have no test today:

- Does `SampleMinimalApi`, the setup users copy, start with `ValidatePipelines()` at the end of the chain, and do its endpoints answer correctly?
- Does the request scope give the endpoint and the handler the same Scoped instance in a running app?
- Does Darker work in a host that has no ASP.NET Core, as its package dependencies promise?

## Proposed Solution

Two checks that behave like a user's application:

1. A test that starts `SampleMinimalApi` in memory and calls each of its endpoints.
2. A console program that starts a host without a web server, runs queries through it and reports success or failure as its exit code.

Both use only Darker's public registration calls, and both run in CI.

## Requirements

### Functional Requirements

#### The sample test

**FR1 — The sample starts.** The test starts `SampleMinimalApi` with startup validation enabled, as the sample's own `Program` configures it. A validation error fails the test at start, with the validation message in the failure.

**FR2 — Query endpoints answer.** `GET /people` returns the repository's people. `GET /people/{id}` returns the name for a known id. Each response has a success status code and the expected body.

**FR3 — Retry recovers a transient fault.** `GET /flaky` returns the sample's success text, which states that the handler succeeded after a transient failure. The handler fails on its first attempt, so the response shows the retry pipeline ran.

**FR4 — Request scope is shared.** `GET /request-scope` returns the same id for the endpoint and for the handler.

**FR5 — The sample is the system under test.** The test uses the sample's own `Program` and registrations. It does not repeat them. A change to the sample's setup is therefore covered without a change to the test.

#### The console harness

**FR6 — Runs without ASP.NET Core.** The harness builds a Generic Host with the public `AddDarker` registration chain, ending with `ValidatePipelines()`. It references no ASP.NET Core package.

**FR7 — Sync, async and stream queries run.** The harness executes one sync query, one async query and one stream query through `IQueryProcessor`. Each returns the expected result.

**FR8 — Retry recovers a transient fault.** A handler that fails on its first call succeeds through a retry pipeline. The harness checks the result.

**FR9 — Scoped dependencies are provided and released.** A handler with a Scoped dependency receives a dependency instance. The host disposes that instance after the query ends.

**FR10 — Invalid configuration fails at start.** A deliberately invalid registration makes host start fail. The failure message names the fault. The harness treats that failure as the expected result of the scenario.

**FR11 — Result is the exit code.** Each scenario returns 0 on success and non-zero on failure, after printing what differed. The harness stops at the first non-zero result and exits with it.

#### CI

**FR12 — The sample test runs with the existing test command.** `dotnet test Darker.Filter.slnf` runs the sample test, on `net8.0` and `net9.0`.

**FR13 — The harness runs in CI.** A CI step runs the harness for `net8.0` and `net9.0`. A non-zero exit code fails the build.

**FR14 — Both projects are in the solution filter.** `Darker.slnx` and `Darker.Filter.slnf` include both new projects, so a build break in either fails CI.

### Non-functional Requirements

**NFR1 — Public API only.** Neither project uses `InternalsVisibleTo`, `Simple*` factories or test registries. Both call only what a user can call.

**NFR2 — No external services.** Neither project needs a network, a database, a fixed port or a separate server process.

**NFR3 — Fast.** The sample test and the harness together add no more than a few seconds to the CI run.

**NFR4 — Deterministic.** The retry scenarios fail by call count, never by delay or timing. The harness uses no waits.

**NFR5 — No cross-test interference.** The sample test does not share a process with another test assembly. One shared host serves all of its tests. This protects the static logger factory that `AddDarker` sets.

**NFR6 — Minimal change to existing code.** `src/` is unchanged. The only change to the sample is one line that lets the test name its `Program` type.

### Constraints and Assumptions

- Both projects reference the product by project reference, not by packed package.
- The harness follows the pattern of `Paramore.Darker.Tests.AOT`: a console program, not a test-runner host.
- The sample's endpoints, registrations and behaviour stay as they are.
- A central package version for the in-memory web test package is added to `Directory.Packages.props`, as Central Package Management requires.
- The solution targets `net8.0` and `net9.0`.
- Each test and scenario is written test-first and is seen failing for the right reason before the code that satisfies it exists.

### Out of Scope

- Benchmarks. A later decision covers them.
- Native AOT coverage of the validation and request-scope paths. `Paramore.Darker.Tests.AOT` is unchanged.
- Consuming Darker from packed NuGet packages. This is deferred, not rejected.
- Any change under `src/`.
- Any change to the sample's behaviour or endpoints.
- Coverage of every Darker feature. The checks cover the sample's four endpoints and the harness's four scenarios (FR7 to FR10).

## Acceptance Criteria

- **AC1** — The sample test fails when the sample's registration is made invalid (for example, a decorator left unregistered), and passes when it is restored. (FR1, FR5)
- **AC2** — Each endpoint test fails when its handler is changed to return a different body. (FR2, FR3)
- **AC3** — The `/request-scope` test fails when `AddDarkerRequestScope()` is removed from the sample. (FR4)
- **AC4** — `dotnet test Darker.Filter.slnf` runs the sample test on both frameworks and passes. (FR12)
- **AC5** — Running the harness returns exit code 0, and returns non-zero when any one scenario is made to fail. (FR6 to FR11)
- **AC6** — The CI workflow runs the harness for both frameworks, and a failing harness fails the workflow. (FR13)
- **AC7** — A search of `src/` and `test/` finds no `InternalsVisibleTo` for either new project. (NFR1)
- **AC8** — `git diff` against `master` shows no change under `src/` and one line changed in the sample. (NFR6)
- **AC9** — The harness project references no ASP.NET Core package or framework. (FR6)

The testing approach is the one in the ADR: mutate the system under test, watch the new check fail, restore it, watch the check pass.

## Additional Context

- Decision: [ADR 0027](../../docs/adr/0027-end-to-end-verification-outside-developer-tests.md).
- Pattern for the harness: `test/Paramore.Darker.Tests.AOT/`.
- The sample under test: `samples/SampleMinimalApi/`.
- Related decisions: ADR 0024 (request-scope adoption), ADR 0025 and 0026 (startup validation).
