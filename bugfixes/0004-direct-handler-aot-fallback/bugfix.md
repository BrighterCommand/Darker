# Native AOT fallback dispatch for direct query handlers

[Issue #410](https://github.com/BrighterCommand/Darker/issues/410) affects handlers that implement `IQueryHandler<,>` or `IQueryHandlerAsync<,>` directly. When a consumer preserved only the public execute method, the trimmer could remove the fallback metadata. A fallback policy then threw `NullReferenceException` when it tried to invoke the missing method.

## Design

The pipeline looks up the named fallback on the statically known open handler interface. This makes the method visible to trim analysis. On invocation, the pipeline binds the method handle to the closed interface for the registered query and result. Calling that interface method also reaches explicit implementations.

The `Interfaces` trimming annotation on `IQueryHandler` preserves the interface list read from the resolved instance. The registered handler type constrains the match when a factory returns a proxy. This puts the metadata requirement at dispatch and covers handlers supplied by type, by a router, or by generic registration. Assembly discovery and public execute-method discovery retain their existing trimming requirements.

The Native AOT regression in `Case3FallbackRunsWhenOnlyExecuteIsRooted` exercises synchronous and asynchronous explicit fallbacks through the query processor. The harness roots only the execute methods and checks that each fallback returns the original query ID.
