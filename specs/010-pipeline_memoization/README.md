# Pipeline Memoization

**Spec ID:** 010-pipeline_memoization
**Created:** 2026-06-23
**Status:** Requirements approved — ready for design (ADR)
**Linked Issue:** [#289](https://github.com/BrighterCommand/Darker/issues/289)

## Overview

Cache the ordered decorator attributes for each query handler once per handler `Type`
(keyed by `Type`, separate sync/async caches, following Brighter), so repeated executions
skip the `GetCustomAttributes` + sort reflection. Handler and decorator instances remain
per-query. A pure performance optimisation with no observable behaviour change.

## Status Checklist

- [x] Requirements (`/spec:requirements`)
- [x] Design / ADR (`/spec:design`) — `docs/adr/0016-pipeline-attribute-memoization.md` (Proposed)
- [ ] Adversarial Review (multiple rounds)
- [ ] Task Breakdown (`/spec:tasks`)
- [ ] Implementation (`/spec:implement`)

## Documents

| Phase | File | Status |
|-------|------|--------|
| Requirements | `requirements.md` | ✅ Approved |
| Design | `docs/adr/0016-pipeline-attribute-memoization.md` | 🟡 Proposed |
| Tasks | `tasks.md` | Not started |
