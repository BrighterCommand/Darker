# Caching Decorator

**Spec ID:** 014
**Created:** 2026-07-20
**Status:** Requirements

## Overview

An opt-in `[CacheableQuery]` decorator that caches query results through the Darker pipeline, built on Microsoft's `HybridCache` abstraction (FusionCache pluggable via DI). Supports sync + async pathways (sync uses an immediate-completion fast return), pluggable cache keys (`IAmCacheable` with a serialization fallback), TTL expiry plus externally-driven tag eviction via a well-known `IQueryContext.Bag` key, and configurable OpenTelemetry hit/miss signals. See `requirements.md` (linked issue [#291](https://github.com/BrighterCommand/Darker/issues/291)).

## Status Checklist

- [ ] **Requirements** — Define WHAT the feature must do (`/spec:requirements`)
- [ ] **Design (ADR)** — Define HOW the feature will be built (`/spec:design`)
- [ ] **Adversarial Review** — Multiple rounds of critical review (`/spec:review`)
- [ ] **Tasks** — Break the design into implementation tasks (`/spec:tasks`)
- [ ] **Implementation** — TDD implementation of tasks (`/spec:implement`)

## Artifacts

| Phase | File | Status |
|-------|------|--------|
| Requirements | `requirements.md` | Drafted (awaiting approval) |
| Design | `design.md` (ADR) | Not started |
| Tasks | `tasks.md` | Not started |

## Notes

_Add any context, links, or decisions here as the spec evolves._
