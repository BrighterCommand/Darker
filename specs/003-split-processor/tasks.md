# Tasks: Split IQueryProcessor into Separate Sync and Async Interfaces

**Spec**: 003-split-processor
**ADR**: [0008-split-query-processor-interface](../../docs/adr/0008-split-query-processor-interface.md)
**Issue**: #288

## Prerequisites

- [x] **STRUCTURAL: Split IQueryProcessor into two interface files**
  - This is a tidy-first structural change — no behavior changes
  - **USE COMMAND**: `/tidy-first split IQueryProcessor.cs into IQueryProcessor (sync-only) and IQueryProcessorAsync (new file)`
  - Current: `src/Paramore.Darker/IQueryProcessor.cs` has both `Execute` and `ExecuteAsync`
  - After:
    - `src/Paramore.Darker/IQueryProcessor.cs` contains only `Execute<TResult>(IQuery<TResult> query)`
    - `src/Paramore.Darker/IQueryProcessorAsync.cs` contains only `ExecuteAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)`
  - `QueryProcessor` declaration changes to `QueryProcessor : IQueryProcessor, IQueryProcessorAsync`
  - All existing tests must still pass after this change

## Core Behavior

- [x] **TEST + IMPLEMENT: Async consumer can resolve and use IQueryProcessorAsync from DI**
  - **USE COMMAND**: `/test-first when async consumer resolves IQueryProcessorAsync from DI should execute query through async pipeline`
  - Test location: "test/Paramore.Darker.Tests/Integrations"
  - Test file: `When_async_consumer_resolves_IQueryProcessorAsync_from_DI_should_execute_query.cs`
  - Test should verify:
    - `IQueryProcessorAsync` resolves from `ServiceProvider` (not null)
    - Calling `ExecuteAsync` on the resolved interface dispatches through the async pipeline and returns the correct result
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In `ServiceCollectionExtensions.AddDarker()`, register `QueryProcessor` as the concrete type
    - Add forwarding registration for `IQueryProcessorAsync` that resolves via `QueryProcessor`
    - Keep existing `IQueryProcessor` registration forwarding to the same `QueryProcessor` instance

- [x] **TEST + IMPLEMENT: Sync consumer can resolve and use IQueryProcessor from DI** (covered by existing AspNetTests.HandlersGetWiredWithServiceCollection)
  - **USE COMMAND**: `/test-first when sync consumer resolves IQueryProcessor from DI should execute query through sync pipeline`
  - Test location: "test/Paramore.Darker.Tests/Integrations"
  - Test file: `When_sync_consumer_resolves_IQueryProcessor_from_DI_should_execute_query.cs`
  - Test should verify:
    - `IQueryProcessor` resolves from `ServiceProvider` (not null)
    - Calling `Execute` on the resolved interface dispatches through the sync pipeline and returns the correct result
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Verify the existing `IQueryProcessor` forwarding registration works after the structural split
    - This may already pass after the structural change + first task's DI registration changes

- [x] **TEST + IMPLEMENT: Both interfaces resolve to the same QueryProcessor instance per scope**
  - **USE COMMAND**: `/test-first when both IQueryProcessor and IQueryProcessorAsync resolved in same scope should be same instance`
  - Test location: "test/Paramore.Darker.Tests/Integrations"
  - Test file: `When_both_interfaces_resolved_in_same_scope_should_be_same_instance.cs`
  - Test should verify:
    - `ServiceProvider.GetRequiredService<IQueryProcessor>()` and `ServiceProvider.GetRequiredService<IQueryProcessorAsync>()` return the same object (reference equality)
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Ensure the DI registration pattern uses a single concrete `QueryProcessor` registration with forwarding for both interfaces

## Consumer Updates

- [x] **TEST + IMPLEMENT: FakeQueryProcessor implements both interfaces for testing**
  - **USE COMMAND**: `/test-first when FakeQueryProcessor used as IQueryProcessorAsync should execute async queries`
  - Test location: "test/Paramore.Darker.Tests"
  - Test file: `When_FakeQueryProcessor_used_as_IQueryProcessorAsync_should_execute_async_queries.cs`
  - Test should verify:
    - `FakeQueryProcessor` can be assigned to `IQueryProcessorAsync`
    - Calling `ExecuteAsync` on `FakeQueryProcessor` via the async interface returns the configured result
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - Update `FakeQueryProcessor` declaration to `FakeQueryProcessor : IQueryProcessor, IQueryProcessorAsync`
    - No method changes needed — it already has both `Execute` and `ExecuteAsync`

## Existing Consumer Migration

- [x] **Update SampleMinimalApi to use IQueryProcessorAsync** (done in structural refactor)
  - `samples/SampleMinimalApi/Program.cs` injects `IQueryProcessor` but only calls `ExecuteAsync`
  - Change injection from `IQueryProcessor` to `IQueryProcessorAsync`
  - Verify the sample builds and runs: `dotnet run --project samples/SampleMinimalApi/SampleMinimalApi.csproj`

- [x] **Update SampleMauiTestApp to use IQueryProcessorAsync** (done in structural refactor)
  - `SampleMauiTestApp/MainPage.xaml.cs` injects `IQueryProcessor` but only calls `ExecuteAsync`
  - Change injection from `IQueryProcessor` to `IQueryProcessorAsync`

- [x] **Update async test classes to use IQueryProcessorAsync** (done in structural refactor)
  - `test/Paramore.Darker.Tests/QueryProcessorAsyncTests.cs`: change `_queryProcessor` type from `IQueryProcessor` to `IQueryProcessorAsync`
  - `test/Paramore.Darker.Tests.AOT/QueryProcessor/AOTQueryProcessorTests.cs`: the `QueryProcessor` property uses `IQueryProcessor` but tests call `ExecuteAsync` — split into separate properties or update to `IQueryProcessorAsync` for async tests
  - `test/Paramore.Darker.Benchmarks/Benchmark.cs`: the `_queryProcessor` field uses `IQueryProcessor` but `BasicAsyncQuery` calls `ExecuteAsync` — needs both interfaces or two fields

- [x] **Update PipelineBuilderExceptionTests to use correct interface per test** (done in structural refactor)
  - `test/Paramore.Darker.Tests/PipelineBuilderExceptionTests.cs`: change `_queryProcessor` type based on which tests use sync vs async, or keep as `QueryProcessor` concrete type since tests exercise both paths

## Verification

- [x] **All tests pass**
  - Run: `dotnet test Darker.Filter.slnf -c Release`
  - All existing tests pass (with interface type updates)
  - New integration tests pass

- [x] **Solution builds clean**
  - Run: `dotnet build Darker.Filter.slnf -c Release`
  - No warnings related to the interface changes
