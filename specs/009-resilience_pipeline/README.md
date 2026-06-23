# Spec 009: resilience_pipeline

**Created:** 2026-06-09
**Status:** Requirements drafted (awaiting approval)
**Linked Issue:** [#293](https://github.com/BrighterCommand/Darker/issues/293)

## Overview

Add first-class Polly V8 `ResiliencePipeline` support to Darker's policies, mirroring
Brighter's additive design — a new `[UseResiliencePipeline]` attribute and decorators
backed by a `ResiliencePipelineRegistry<string>`, while keeping the existing legacy Polly
policy API (`[RetryableQuery]`, `Policies(...)`) unchanged.

## Status Checklist

- [x] Requirements (`/spec:requirements`)
- [ ] Design / ADR (`/spec:design`)
- [ ] Adversarial Review (multiple rounds)
- [ ] Task Breakdown (`/spec:tasks`)
- [ ] Implementation (`/spec:implement`)

## Artifacts

| Phase | File | Status |
|-------|------|--------|
| Requirements | `requirements.md` | Drafted (awaiting approval) |
| Design | `design.md` | Not started |
| Tasks | `tasks.md` | Not started |
