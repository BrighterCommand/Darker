# Requirements: Split IQueryProcessor into Separate Sync and Async Interfaces

> **Note**: This document captures user requirements and needs. Technical design decisions and implementation details should be documented in an Architecture Decision Record (ADR) in `docs/adr/`.

**Linked Issue**: #288

## Problem Statement

As a developer using Darker, I would like separate interfaces for sync and async query processing, so that I can depend only on the execution model my application actually uses.

Currently `IQueryProcessor` defines both `Execute<TResult>` and `ExecuteAsync<TResult>` on a single interface. Most modern applications only use async. This violates the Interface Segregation Principle (ISP) by forcing consumers to depend on methods they do not use. ADR 0005 already recognises that sync and async are separate paths through the pipeline, yet the public contract conflates them into one interface.

## Proposed Solution

Split `IQueryProcessor` into focused interfaces:

- A sync-only interface exposing `Execute<TResult>`
- An async-only interface exposing `ExecuteAsync<TResult>`
- `QueryProcessor` continues to implement both interfaces, so DI registration remains backwards compatible
- Consumers who want the unified (current) contract can still obtain it

## Requirements

### Functional Requirements

- **FR1**: Provide an async-only query processor interface with `ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)`.
- **FR2**: Provide a sync-only query processor interface with `Execute<TResult>(IQuery<TResult> query)`.
- **FR3**: `QueryProcessor` must implement both the sync and async interfaces.
- **FR4**: Consumers must be able to inject either the sync-only or async-only interface. Consumers who need both paths inject both interfaces.
- **FR5**: The DI registration in `AddDarker()` must register `QueryProcessor` against both interfaces so existing and new injection patterns both resolve correctly.

### Non-functional Requirements

- **NFR1**: No runtime performance impact; the change is purely at the contract (interface) level.
- **NFR2**: Minimal migration effort for existing consumers who inject `IQueryProcessor` today.

### Constraints and Assumptions

- The concrete `QueryProcessor` class continues to live in `Paramore.Darker`.
- The `Paramore.Darker.Extensions.DependencyInjection` package must be updated to register `QueryProcessor` against the new interfaces.
- This is a V5 breaking change. Consumers injecting `IQueryProcessor` and calling `ExecuteAsync` will need to change their injection type to the async interface.

### Out of Scope

- Splitting the internal pipeline (`PipelineBuilder`, handler resolution) into separate sync/async implementations. The pipeline internals are already split; this spec covers only the public-facing processor interface.
- Providing a combined interface that inherits both sync and async — this would undermine the purpose of the split.
- Changes to handler interfaces (`IQueryHandler`, `IQueryHandlerAsync`).

## Acceptance Criteria

- **AC1**: A consumer can inject the async-only interface and call `ExecuteAsync` without any reference to sync methods.
- **AC2**: A consumer can inject the sync-only interface and call `Execute` without any reference to async methods.
- **AC3**: A consumer who needs both paths can inject both interfaces.
- **AC4**: The existing test suite passes (with updates to use the correct interface).
- **AC5**: The `AddDarker()` extension method registers `QueryProcessor` against both interfaces.
- **AC6**: The sample application (`SampleMinimalApi`) continues to build and run correctly.

## Additional Context

- Discussed in the V5 roadmap: https://github.com/BrighterCommand/Darker/discussions/273
- Aligns with ADR 0005's recognition that sync and async are separate paths.
- This is part of a broader V5 effort to modernise the Darker API surface.
