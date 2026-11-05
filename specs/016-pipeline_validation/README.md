# Pipeline Validation

**Spec ID:** 016
**Created:** 2026-10-19
**Status:** Requirements

## Overview

Add `ValidatePipelines()`, which checks at startup that every registered query type has a resolvable handler and decorators, and that the handler matches the execution path (sync/async), so configuration errors surface at startup rather than at first query execution. Modelled on Brighter's `PipelineValidator`. See issue [#305](https://github.com/BrighterCommand/Darker/issues/305), and V5 discussion [#273](https://github.com/BrighterCommand/Darker/discussions/273).

Lifetime validation was deliberately left out of spec 015 (#377) and belongs here.

## Status Checklist

- [ ] **Requirements** — Define WHAT the feature must do (`/spec:requirements`)
- [ ] **Design (ADR)** — Define HOW the feature will be built (`/spec:design`)
- [ ] **Adversarial Review** — Multiple rounds of critical review (`/spec:review`)
- [ ] **Tasks** — Break the design into implementation tasks (`/spec:tasks`)
- [ ] **Implementation** — TDD implementation of tasks (`/spec:implement`)

## Artifacts

| Phase | File | Status |
|-------|------|--------|
| Requirements | `requirements.md` | Not started |
| Design | ADRs (`.adr-list`) | Not started |
| Tasks | `tasks.md` | Not started |

## Notes

_Add any context, links, or decisions here as the spec evolves._
