# ADR Frontmatter Conventions

This document defines the YAML frontmatter that every Architecture Decision Record (ADR) in
`docs/adr/` carries. It is the single source of truth referenced by the `read_adr_metadata` and
`write_adr_metadata` skills, by `/spec:design` and `/adr`, and by the backfill process that added
frontmatter to the existing ADRs.

## Why frontmatter

Agents (and people) need to find prior ADRs relevant to a new design without reading every ADR in
full. Grepping whole ADR bodies is expensive in tokens and pollutes context — especially with
superseded decisions. A small, structured metadata block at the top of each ADR lets a reader scan
`title` + `summary` + `tags` cheaply and decide which ADRs are worth opening.

This convention is shared with Brighter, which records the decision behind it in its ADR *"Add
Frontmatter to ADRs"*.

## Schema

Frontmatter is a YAML block delimited by `---` fences, placed at the very top of the file, before
the `# N. Title` heading:

```yaml
---
id: 0021-caching-decorator-architecture
title: "Caching Decorator Architecture"
status: Accepted
author:
  - "Ian Cooper"
created: 2026-07-20
summary: "Adds a caching decorator over Microsoft's HybridCache abstraction that short-circuits the pipeline on a hit, with a replaceable ICacheKeyGenerator, a required expiry, and hit/miss recorded as a span attribute."
tags:
  - "decorators"
  - "caching"
  - "performance"
---

# 21. Caching Decorator Architecture

Date: 2026-07-20
...
```

## Field reference

| Field     | Required | Type            | Notes |
|-----------|----------|-----------------|-------|
| `id`      | yes      | string          | **Stable identity = the filename stem** (name without `.md`), e.g. `0021-caching-decorator-architecture`. This is the unique key; see *Identity and numbering* below. |
| `title`   | yes      | string (quoted) | The ADR title. Use the H1 title text (without the leading `N.` number). |
| `status`  | yes      | enum            | One of `Proposed`, `Accepted`, `Deprecated`, `Superseded`. Mirrors the body `## Status`. See *Status* below. |
| `author`  | yes      | list of strings | Always list form, even for one author: `- "Name"`. Backfill default is `- "Darker Team"` where no author is recorded. |
| `created` | yes      | date (ISO 8601) | `YYYY-MM-DD`. For existing ADRs use the body `Date:` line. |
| `summary` | yes      | string (quoted) | One or two sentences describing the decision. This is what an agent reads to decide whether to open the full ADR — make it specific about *what was decided*, not just the topic. |
| `tags`    | yes      | list of strings | 1–4 topic tags drawn from the controlled vocabulary below. |

## Identity and numbering

The ADR **number is a non-unique ordering hint, not an identity.** ADR numbers are allocated by each
branch picking `max+1` without locking, so parallel branches can collide: `0020` is already shared
by `0020-agreement-dispatch-handler-routing` and `0020-validation-decorator-architecture`. We do
**not** renumber — the inbound references (inter-ADR links, `release_notes.md`, `specs/`) and
historical anchors make renumbering costly and destructive.

Instead:

- The **identity is the `id` field = the filename stem** (number + slug), which *is* unique.
- Tooling (`read_adr_metadata`, the generated index) keys off `id`/slug, never the bare number.
- New ADRs still take `max+1` as a number for ordering, and a number collision with a concurrent
  branch is **acceptable** — it is not a defect to fix, because identity is the slug.

When you refer to an ADR in prose or `Related ADRs`, prefer the slug/filename
(`0020-validation-decorator-architecture`) over the bare number where ambiguity is possible.

## Status

Canonical values (Nygard vocabulary):

- **Proposed** — drafted, not yet approved. New ADRs start here (set by `/spec:design` / `/adr`).
- **Accepted** — approved and in force. `/spec:approve design` flips `Proposed` → `Accepted` in
  both the frontmatter `status` and the body `## Status`.
- **Deprecated** — no longer in force, but *not* replaced by a specific ADR (the decision was
  retired/abandoned). Use this when there is no superseding ADR to point at.
- **Superseded** — replaced by a *specific* later ADR. When a new ADR supersedes an older one, set
  the older ADR's `status` to `Superseded` and add a `Superseded by` reference to the superseding ADR.

`read_adr_metadata` skips both `Deprecated` and `Superseded` ADRs by default when surfacing prior art.

### Backfill status normalization

The legacy ADR bodies use inconsistent status prose. Backfill normalizes to the canonical values in
**both** the frontmatter `status` **and** the body `## Status` line (this is an intentional cleanup):

| Body text (legacy)                                                  | Canonical `status` |
|---------------------------------------------------------------------|--------------------|
| `Accepted`, `Adopted`, `Approved`, `Modified`, `Accepted. Amended …`| `Accepted`         |
| `Proposed`, `Proposal`, `Draft`, `Proposed (Draft PR …)`            | `Proposed`         |
| `Retired`                                                           | `Deprecated`       |
| An ADR explicitly replaced by a *specific* later ADR                | `Superseded`       |

Normalization rules:
- Where a status line carries extra information (e.g. `Accepted. Amended 2026-02-17 to add …`),
  normalize the status **word** to the canonical value but preserve the
  amendment sentence in the body (move it below the status line if needed — do not lose it).
- **Partial** supersession (a later ADR supersedes only a *section* of an older one) does **not** change the older ADR's status — it stays `Accepted`; note the
  partial supersession in prose instead.
- Do **not** guess. Only assign `Superseded` when a specific replacing ADR exists; only assign
  `Deprecated` for a genuinely retired decision. Flag anything unclear for human confirmation.

## Tag taxonomy (controlled vocabulary)

Tags are lowercase kebab-case, quoted, 1–4 per ADR, drawn from this controlled list. Prefer the
most specific applicable tags. If no existing tag fits, propose an addition to this list rather than
inventing an ad-hoc tag (keeps the vocabulary from sprawling).

**Architecture & API**
`architecture`, `cqrs`, `pipeline`, `decorators`, `api-design`, `configuration`, `di`, `lifetime`,
`dispatch`, `streaming`, `async`, `query-context`

**Reliability & validation**
`resilience`, `retry`, `fallback`, `circuit-breaker`, `error-handling`, `validation`

**Performance**
`performance`, `caching`, `memory`, `concurrency`

**Observability**
`observability`, `otel`, `tracing`, `metrics`, `logging`

**Packaging & dependencies**
`packaging`, `dependencies`, `serialization`

**Process & tooling**
`meta`, `testing`

## Skills

- **`read_adr_metadata`** — extracts frontmatter blocks cheaply and returns matching ADRs
  (`id` + `title` + `summary` + path) filtered by tag/status. Suggested from `/spec:design` to find
  prior art before drafting a new ADR. Skips `Superseded` by default.
- **`write_adr_metadata`** — adds or updates a file's frontmatter and enforces the status rules
  (new = `Proposed`; supersession sets the old ADR to `Superseded`). Idempotent.

## Derived index

`docs/adr/index.md` is a table generated *from* the frontmatter (id, title, status, tags, summary).
It is a derived cache for single-file reads — it must never be hand-edited; regenerate it from
frontmatter so it cannot drift. The generated file carries a `DO NOT EDIT` banner.

**Regenerate** it after adding an ADR or changing any frontmatter (status, tags, summary, title).
This is the single canonical command — `/adr` and `/spec:approve` call it, and you can run it by
hand:

```bash
awk -f .claude/commands/adr/generate_adr_index.awk docs/adr/[0-9]*.md > docs/adr/index.md
```

The generator reads **only** the frontmatter block of each ADR (like `read_adr_metadata`), keys rows
off `id`, and orders by filename. It is deterministic: rerunning it with no ADR changes produces no
diff, so a dirty `index.md` after regeneration means an ADR's frontmatter actually changed.
