# End-to-End Verification

**Spec ID:** 017
**Created:** 2026-11-05
**Status:** Requirements

## Overview

Verify V5 end to end outside developer tests, using two real-host projects that call only public API and use project references: an xunit test that starts `SampleMinimalApi` in memory with `WebApplicationFactory`, and a console harness that runs queries in a Generic Host with no web server. The decision is recorded in [ADR 0027](../../docs/adr/0027-end-to-end-verification-outside-developer-tests.md) (Accepted).

Out of scope: benchmarks, native AOT coverage of the validation and request-scope paths, and consuming packed NuGet packages.

## Status Checklist

- [ ] **Requirements** — Define WHAT the feature must do (`/spec:requirements`)
- [x] **Design (ADR)** — ADR 0027 accepted before this spec was created
- [ ] **Adversarial Review** — Multiple rounds of critical review (`/spec:review`)
- [ ] **Tasks** — Break the design into implementation tasks (`/spec:tasks`)
- [ ] **Implementation** — TDD implementation of tasks (`/spec:implement`)

## Artifacts

| Phase | File | Status |
|-------|------|--------|
| Requirements | `requirements.md` | Not started |
| Design | ADRs (`.adr-list`) | ADR 0027 accepted |
| Tasks | `tasks.md` | Not started |

## Notes

- The ADR predates the requirements. Requirements should be written to match it, and any gap they expose goes back to the ADR as an amendment, not a silent change.
