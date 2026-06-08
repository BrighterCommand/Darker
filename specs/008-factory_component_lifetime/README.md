# Spec 008: Factory Component Lifetime

**Created:** 2026-06-06
**Issue:** [#329](https://github.com/BrighterCommand/Darker/issues/329)
**Branch:** `329-factory-component-lifetime`
**Target:** V5 (breaking changes permitted)

## Summary

Make Darker's DI handler/decorator factories lifetime-aware
(Singleton / Scoped / Transient), matching Brighter, so that handlers and
decorators — and their scoped dependencies such as an EF Core `DbContext` —
are created and released according to their configured lifetime.

## Motivating Problems (from issue #329)

1. **Singleton handlers get disposed after one query.** `ServiceProviderHandlerFactory.Release`
   blindly disposes any `IDisposable`, so a singleton handler is disposed after the
   first query and the next query resolves a disposed instance.
2. **Scoped dependencies resolve from the wrong scope.** With the default singleton
   `QueryProcessor`, the factory captures the root provider, so scoped handler/decorator
   dependencies (e.g. EF Core `DbContext`) never bind to the request scope.

## Status

- [ ] Requirements (`/spec:requirements`)
- [ ] Design / ADR (`/spec:design`)
- [ ] Adversarial Review (`/spec:review`) — multiple rounds
- [ ] Tasks (`/spec:tasks`)
- [ ] Implementation (`/spec:implement`)

## Next Step

Run `/spec:requirements` to capture the requirements specification.
