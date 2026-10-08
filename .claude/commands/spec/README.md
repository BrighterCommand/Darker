# Specification-Driven Development Commands

This directory contains Claude Code commands that implement a specification-driven development workflow for Darker contributions. These commands help you follow Darker's preferred contribution workflow: **Issue -> Requirements -> ADR(s) -> Tasks -> Tests -> Code**.

## Overview

The spec commands provide a structured approach to designing and implementing features:

1. **Requirements**: Capture user needs and problem statements
2. **Design (ADRs)**: Document architectural decisions (can have multiple ADRs per requirement)
3. **Tasks**: Break down implementation into actionable steps
4. **Implementation**: Follow TDD to write tests and code, in whichever **review gear** the work
   currently warrants — shifted with `/spec:gear`, armed by default

## Workflow

```
Specification Workflow

 GitHub Issue
      |
      v
 Requirements.md ---------> /spec:requirements [issue-number]
      |                      /spec:approve requirements
      |
      v
 ADR(s) in docs/adr/ -----> /spec:design [focus-area]
      |                      /spec:review design [adr-number]
      |                      /spec:approve design [adr-number]
      |                      (Repeat for multiple architectural decisions)
      |
      v
 tasks.md -----------------> /spec:tasks
      |                      /spec:review tasks
      |                      /spec:approve tasks
      |
      v
 Pick a driver, ----------> /spec:implement        (sonnet; one task at a time)
 shift gear as you go       /spec:ralph-implement  (opus + auto mode; unattended loop)
      |                           ^
      |                           +-- /spec:gear  shifts the review gear, either way
      v
 Pull Request
```

**One task list, one gear lever.** There is a single task list — `tasks.md` — and both drivers run
it. What changes between "attended" and "unattended" is the **review gear**, and the gear is
shiftable at any point in either direction (Brighter's [ADR 0071](https://github.com/BrighterCommand/Brighter/blob/master/docs/adr/0071-tdd-review-gear.md)):

| Gear | Gate | Meaning |
|------|------|---------|
| `review-before` | ✅ armed | Every test reviewed in the IDE before implementation. **The default.** |
| `review-after` | ➖ not armed | RED still proved first; the work is reviewed as a batch afterwards. |

**Gears, not paths.** You select a gear for the certainty you have and the blast radius you face.
Early in a spec, while the design is still moving, run `review-before`. Later, on a run of
near-identical tasks whose shape has been approved several times, upshift. The moment the work
starts producing tests you would not have approved, downshift — mid-section, with nothing unwound.

The two drivers:

- **`/spec:implement`** — one task at a time in the main agent on **sonnet**. Honours whatever gear
  is set.
- **`/spec:ralph-implement`** — a self-driving unattended loop over the **same** `tasks.md`, on
  **opus** under **auto mode**, delegating each task to a **sonnet** sub-agent. Always
  `review-after`; a downshift stops it cleanly.

Neither regenerates a task list, and switching between them costs nothing.

## Sub-agents & model policy

Some commands delegate their reasoning-heavy or implementation-heavy work to a **sub-agent**
(launched via the `Agent` tool) rather than doing it inline. This keeps the main conversation's
context clean and gives the heavy work a focused, single-purpose context.

**The convention** (modelled on `/spec:review`):

1. **The main agent gathers inputs.** A sub-agent starts with a clean context — it only knows
   what is in its prompt. The command reads the needed files (and runs `gh`/`git`) first, then
   passes the text or paths.
2. **Launch `Agent`** with an explicit `subagent_type` and `model`:
   - **`/spec:tasks`** uses `subagent_type: "Plan"`. `Plan` has all tools **except** `Agent`,
     `ExitPlanMode`, `Edit`, `Write`, and `NotebookEdit` — so it can Read/Glob/Grep/Bash but has
     no file-editing tool. That makes it much **harder** for the sub-agent to accidentally write
     the spec file than relying on the prompt alone (it still has `Bash`, so the prompt also
     forbids writing through it).
   - **`/spec:review`** uses `subagent_type: "general-purpose"` (adversarial reasoning that
     needs no source mutation).
   - **`/spec:ralph-implement`** uses `subagent_type: "general-purpose"` because its per-task
     sub-agent genuinely *writes* source files.
3. **The main agent owns all user interaction.** A sub-agent is one-shot — once launched it
   runs to completion and returns; it cannot pause to ask the user anything. So before
   launching, the main agent clarifies any ambiguous inputs with the user via `AskUserQuestion`,
   then launches the sub-agent with the clarified inputs folded in. The
   `Plan`-based `tasks` sub-agent has no `AskUserQuestion` so it *structurally* can't prompt; the
   `general-purpose` `review` sub-agent is explicitly instructed not to. **Exception:**
   `/spec:ralph-implement` runs fully **unattended** — neither its orchestrator nor its
   sub-agent prompts the user once the loop starts.
4. **The sub-agent RETURNS its artifact as text** — it does *not* write the spec file. The one
   exception is `/spec:ralph-implement`, whose sub-agent must write the test and implementation
   source files (it still never commits or edits the task list).
5. **The main agent validates** the returned output against a checklist, then writes the file
   and does all bookkeeping (approval markers, `.adr-list`, git, next-steps).

The remaining planning commands (`/spec:requirements`, `/spec:design`) currently run inline in the
main agent rather than delegating — they have no sub-agent to assign a model to.

**Model policy** — reasoning vs. implementation:

| Command | Sub-agent (type) | Model | Rationale |
|---------|------------------|-------|-----------|
| `/spec:tasks` | Yes — `Plan` (read-only) | **opus** | Planning / coverage mapping |
| `/spec:review` | Yes — `general-purpose` | **opus** | Adversarial reasoning |
| `/spec:ralph-implement` (orchestrator) | — (the loop itself) | **opus** | Cheap bookkeeping + **required for auto mode** |
| `/spec:ralph-implement` (per-task sub-agent) | Yes — `general-purpose` (writes source) | **sonnet** | Mechanical TDD implementation, kept off the opus loop context for cost |
| `/spec:implement` | No | **sonnet** (Step 0 prompts to switch) | Implementation work; runs in the main agent, so set the session model |
| `/spec:requirements`, `/spec:design` | No (main agent) | — | Run inline |
| `/spec:new`, `/spec:switch`, `/spec:approve`, `/spec:status`, `/spec:gear`, `/spec:write_release_notes` | No | — | Mechanical bookkeeping |

`/spec:ralph-implement` runs **two models on purpose**: the orchestrator loop on **opus**
(required for **auto mode**, and the policy for the unattended path) does only cheap
bookkeeping — STOP-file and gear checks, task selection, marking checkboxes, committing, counting — while
each task's actual test + implementation is delegated to a **sonnet** sub-agent. That keeps the
expensive per-task churn on the cheaper model and out of the opus loop's context, lowering cost
without taking the orchestrator off opus.

`/spec:implement` is deliberately **not** delegated: its per-behavior
Red → user-approval → Green → Refactor loop is interactive, and the approval gate — armed by
default — must run in the main agent where it can reach the user. Because there is no sub-agent to assign a model
to, run the command itself on **sonnet** (the session model) — it is implementation work. **Step
0 of `/spec:implement` actively checks the session model and prompts you to switch to sonnet if
you are on another model** (e.g. opus).

## Commands

### `/spec:new <feature-name>`

Create a new specification for a feature.

```bash
/spec:new query-caching
```

---

### `/spec:requirements [issue-number]`

Create or update requirements specification for the current spec.

```bash
/spec:requirements 123     # From GitHub issue
/spec:requirements          # From scratch
```

---

### `/spec:design [focus-area]`

Create an Architecture Decision Record (ADR) for a specific architectural decision.

```bash
/spec:design query-caching
/spec:design decorator-pipeline
/spec:design handler-lifecycle
```

Before drafting, surfaces prior-art ADRs with `read_adr_metadata` (frontmatter only, retired ADRs
skipped). After writing, stamps the new ADR's YAML frontmatter with `write_adr_metadata`
(`status: Proposed`, a summary, 1–4 tags from the taxonomy in
`.agent_instructions/adr_frontmatter.md`), regenerates `docs/adr/index.md`, and — if the ADR's
`## Consequences` record a breaking change — recommends `/spec:write_release_notes`.

---

### `/spec:approve <phase> [adr-number]`

Approve a specification phase or specific ADR.

```bash
/spec:approve requirements
/spec:approve design          # Approve all ADRs
/spec:approve design 0043     # Approve specific ADR
/spec:approve tasks
```

Approving the **design** flips each ADR to `Accepted` with `write_adr_metadata` (frontmatter and
body `## Status` together), marks any ADR it replaces `Superseded` (or `Deprecated`), and
regenerates `docs/adr/index.md`. It then points you at `/spec:tasks` — there is one task list and one route to
it. Attended vs. unattended is not a fork here; it is a **gear** you pick (and change) at
implementation time with `/spec:gear`.

Approving **tasks** freezes the **content** of `tasks.md`. Checkbox state (`[ ]` → `[x]`/`[!]`) is
progress bookkeeping and both drivers write it; rewording, adding, removing or reordering tasks
after that point needs a fresh review.

---

### `/spec:review [phase] [adr-number]`

Review the current specification phase or specific ADR.

```bash
/spec:review                  # Auto-detect phase
/spec:review requirements
/spec:review design 0043
/spec:review tasks
```

---

### `/spec:tasks`

Create the implementation task list based on the approved design. This is the **single** task
list; both `/spec:implement` and `/spec:ralph-implement` run it. Drafting is delegated to a `Plan`
sub-agent on **opus**, which returns the list plus an FR/ADR coverage cross-reference; the main agent
sanity-checks coverage and writes `tasks.md`.

```bash
/spec:tasks
```

Every task opens with a tag — `TEST + IMPLEMENT`, `CHARACTERISE`, `STRUCTURAL`, `SETUP`, `DOC` or
`VERIFY` — and behavioral tasks carry the `⛔` approval-gate line, which fires in the default
`review-before` gear. Do not encode the gear in `tasks.md`.

---

### `/spec:write_release_notes [spec-id]`

Write, or replace in place, the spec's section of the root `release_notes.md`. The section sits
under the file's first `##` heading (`## Master`), is headed `### {title} (spec {id})`, and is marked
with `<!-- spec: {spec directory} -->` on the next line so later runs find and replace it rather
than duplicating it.

```bash
/spec:write_release_notes                         # the current spec
/spec:write_release_notes 014-Caching-Decorator   # a named spec (full name, numeric id, or substring)
```

Breaking-change items are judged from the prose of each ADR's `## Consequences` (and
`requirements.md`), never from a diff. The command stops without writing whenever it would have to
guess — an ambiguous spec id, a missing `release_notes.md`, a duplicate or misplaced marked section,
or an unmarked heading that already names the spec. It never stages or commits.

`/spec:design` recommends it when an ADR records a breaking change, and `/spec:review design` flags a
breaking change with no marked section.

---

### `/spec:status`

Show status of all specifications, including the resolved review gear of the active spec (and of
any spec not in the default gear).

```bash
/spec:status
```

---

### `/spec:switch <spec-name>`

Switch to a different specification.

```bash
/spec:switch 0002-another-feature
```

---

### `/spec:implement [task-number] [--review-before|--review-after]`

Begin TDD implementation of the approved specification, one task at a time. Recommended on
**sonnet**; Step 0 prompts you to switch if the session is on another model.

```bash
/spec:implement                 # All tasks
/spec:implement 3               # Specific task
/spec:implement --review-after  # Shift the gear and start in the same gesture (sugar for /spec:gear)
```

**Requirements:** Tasks approved (`.tasks-approved`) and all ADRs Accepted.

**Gear-aware.** The command resolves the review gear before every task — so a shift made from
another terminal takes effect at the next task, in either direction. It announces the resolved gear
and the reason it was shifted before starting.

- In `review-before` (the default): Red → **user approval** → Green → Refactor. No implementation
  is written without approval of the test.
- In `review-after`: the pause is skipped — and *only* the pause. RED must still be proved first,
  and the run says so in one line. It still stops and asks if the test looks wrong, needs a design
  decision, or duplicates an existing test.

**Two commits per task, in both gears**: a `feat:`/`test:`/`fix:`/`refactor:` commit for the change,
then a separate `docs:` commit ticking the task off in `tasks.md`.

---

### `/spec:gear [review-before|review-after] ["section"] [--because "..."]`

Report or shift the **review gear** — whether `/test-first`'s approval gate is armed for the
current spec. See [`gear.md`](gear.md) and Brighter's [ADR 0071](https://github.com/BrighterCommand/Brighter/blob/master/docs/adr/0071-tdd-review-gear.md).

```bash
/spec:gear                                        # what gear am I in, and why?
/spec:gear review-after "Phase 2 — Cache key generation (behaviour)" \
    --because "Phase 1's five tests were all approved unchanged; the shape is settled"
/spec:gear review-before --because "the last two tests asserted the wrong thing"
```

**The gear file** — `specs/{current-spec}/.current-gear`, **gitignored**:

```
# Working state — untracked. Shift with /spec:gear.
gear: review-after
scope: Phase 2 — Cache key generation (behaviour)
driver: ralph
shifted: 2026-10-07 — Phase 1's five tests were all approved unchanged; the shape is settled
```

| Field | Required | Meaning |
|-------|----------|---------|
| `gear` | yes | `review-before` \| `review-after` |
| `scope` | no | A section of `tasks.md` — a heading's text (any depth) or a `tasks N-M` range for flat lists. Absent = spec-wide. |
| `driver` | no | `implement` \| `ralph` |
| `shifted` | yes | Date + one-line reason |

**Resolution** — absent file, unparseable file, unknown value, a task outside `scope:`, or a
`scope:` that matches nothing in `tasks.md` all resolve to `review-before`. Fail safe, never fail
open.

**Why untracked**: the gear is where the work currently *is*, not what the project decided.
**Why per-spec**: a root-level gear file would survive branch switches and silently disarm the gate
for unrelated work.

**Who honours it**: `/spec:implement` and `/spec:ralph-implement`. A standalone `/test-first` and
`/bugfix:test` are **always gated** — they never read the file.

**What `review-after` does NOT remove**: RED-first, the full regression suite, the two-commit shape,
and every test-authoring convention. Only the human pause goes.

**Discoverability**: because the file is untracked, `/spec:gear` also maintains a pointer line in
`PROMPT.md`, and `/spec:status` reports the resolved gear.

---

### `/spec:ralph-implement [count]`

Unattended TDD implementation from the spec's **approved `tasks.md`** — the same task list
`/spec:implement` works from — via a **self-driving loop** in the `review-after` gear. Run it on
**opus** with **auto mode** enabled for a true unattended run.

```bash
# Ask the run bound up front, then loop unattended
/spec:ralph-implement

# Shortcut: pre-set the tasks bound to 3 (skips the bound prompt)
/spec:ralph-implement 3
```

**No separate task list.** This command reads `tasks.md`, skips tasks already ticked, and honours
the gear's `scope:` if one is set. Nothing is regenerated; there is no `ralph-tasks.md` (older specs
may still hold one from the retired `/spec:ralph-tasks` command — it is historical).

**Up-front setup (Step 0, the only interactive part):**
- Advises that the orchestrator should be on **opus** and that **auto mode** should be on
  (auto mode is a permission mode set in Claude Code settings / `CLAUDE_CODE_ENABLE_AUTO_MODE`,
  Opus-gated — the command can't toggle it; it advises and proceeds).
- **Sets the gear** to `review-after`, asking for the reason (and optionally a section scope) if
  the spec is not already in that gear, so `/spec:gear` and `/spec:status` tell the truth mid-run.
- Asks (via `AskUserQuestion`) which **run bound** to use — *unless* a `count` was passed,
  which sets the tasks bound directly:
  - **Tasks** — stop after N tasks complete (the `count` argument)
  - **Turns** — stop after N loop iterations attempted (failures included)
  - **Budget** — stop after ~N output tokens consumed

**Two models on purpose:** the **opus** orchestrator does only bookkeeping; each task's test +
implementation is delegated to a **sonnet** sub-agent (cheaper, and kept off the opus loop
context). The sub-agent never commits, pushes, or edits the task list.

**Task shapes.** The loop matches the task's leading label case-insensitively and treats synonyms
as one shape:

| Shape | Labels | Commit |
|-------|--------|--------|
| Behavioural | `TEST + IMPLEMENT` | `feat:` / `fix:` |
| Test only | `TEST`, `TEST (RED)` | `test:` |
| Implementation only | `IMPLEMENT` | `feat:` — **only** when the specifying test already exists; otherwise skipped |
| Structural | `TIDY FIRST`, `TIDY`, `TIDY-FIRST`, `STRUCTURAL` | `refactor:` |
| Characterisation | `CHARACTERISE` | `test:` — RED observed under the task's named production mutation |
| Documentation | `DOC`, `DOCS`, `DOCUMENT` | `docs:` |
| Scaffolding | `SETUP` | `chore:` |
| Checkpoint | `VERIFY`, `VALIDATION` | bookkeeping tick only |

Anything else it marks `- [!]` and skips rather than improvising.

**Loop per task:** check `RALPH_STOP` and the gear → select next in-scope `- [ ]` task → delegate
🔴 Red → 🟢 Green → 🔵 Refactor to a sonnet sub-agent → orchestrator commits the change, then
commits the checkbox tick → check continuation → repeat. Long runs can self-pace across context
windows with `ScheduleWakeup`.

**Stop mechanisms:**
- The chosen **bound** (tasks / turns / budget)
- `/spec:gear review-before` — **downshift**: the loop finishes the task in flight, then stops
  cleanly with `DOWNSHIFTED`. Nothing completed is unwound; `/spec:implement` resumes from the next
  unchecked task under the restored gate
- `RALPH_STOP` file at repo root — the unattended kill-switch (`touch RALPH_STOP` from another
  terminal); halts after the current task
- **Esc** — cancel a pending self-paced wake-up at the keyboard
- **Scope exhausted** — the next unchecked task falls outside the gear's `scope:`
- Automatically stops when all tasks complete

**Error handling:** Failed tasks are marked `- [!]` with an explanation and skipped.

**What it does not drop:** RED is still proved before any production code, the full regression
suite still runs, each task still produces the two-commit shape, and every test-authoring
convention still applies. `review-after` removes the human pause and nothing else.

**Requirements:**
- Requirements, design **and tasks** approved (`.requirements-approved`, `.design-approved`,
  `.tasks-approved`)
- All ADRs must be approved (Status: Accepted)
- Recommended: session on **opus** with **auto mode** enabled

---

### Running the unattended loop

The loop runs **in-session** — no external bash runner. Built-in **auto mode** removes the
per-action permission prompts and the self-driving loop (optionally self-paced with
`ScheduleWakeup`) replaces an overnight runner.

```bash
# 1. The normal spec workflow, through to an APPROVED TASK LIST
/spec:requirements 123
/spec:approve requirements
/spec:design query-caching
/spec:approve design
/spec:tasks
/spec:review tasks
/spec:approve tasks

# 2. Work the early, uncertain tasks attended — the gate is armed by default
/spec:implement

# 3. Once the task shape is settled, upshift and hand the rest to the loop
/spec:gear review-after "Phase 2 — Cache key generation (behaviour)" \
    --because "Phase 1's five tests were all approved unchanged"
/model opus
/spec:ralph-implement         # choose tasks / turns / budget when prompted

# 4. Take back per-test review at any point, from another terminal
/spec:gear review-before      # loop stops after the current task; nothing is unwound
/spec:implement               # resumes from the next unchecked task, gated

# 5. Or stop the loop outright
touch RALPH_STOP              # unattended kill-switch (halts after current task)
#   …or press Esc to cancel a pending self-paced wake-up
```

Optionally drive it with the built-in `/loop` instead, e.g. `/loop /spec:ralph-implement`
(self-paced) — the same command, repeated by `/loop`.

## File Structure

```
Darker/
├── specs/
│   ├── .current-spec                      # Tracks active spec
│   └── 0001-feature-name/
│       ├── .issue-number                  # GitHub issue number
│       ├── .requirements-approved         # Approval marker
│       ├── .design-approved               # Approval marker
│       ├── .tasks-approved                # Approval marker
│       ├── .adr-list                      # List of associated ADRs
│       ├── requirements.md                # User requirements
│       ├── tasks.md                       # Implementation tasks (the single task list)
│       ├── .current-gear                  # Review gear — GITIGNORED working state
│       └── README.md                      # Spec overview
├── docs/
│   └── adr/
│       ├── index.md                       # Derived index (generated from frontmatter; do not hand-edit)
│       ├── 0001-record-architecture-decisions.md
│       ├── 0002-feature-aspect-one.md
│       └── 0003-feature-aspect-two.md
└── release_notes.md                       # Per-spec release-notes sections (/spec:write_release_notes)
```

## Best Practices

### Requirements
- Frame problem as user story: "As a [user] I want [capability] so that [benefit]"
- Focus on WHAT users need, not HOW to implement
- Keep it concise - technical details go in ADRs

### ADRs (Architecture Decision Records)
- **One architectural decision per ADR** - stay focused
- Focus on WHY, not just WHAT
- Document alternatives considered and why they were rejected
- First ADR should be first commit on feature branch

### Tasks
- Break down into small, testable increments
- Follow TDD: write tests before implementation
- Identify dependencies between tasks
- Give sections meaningful, stable headings — `/spec:gear` can scope a gear to a single section,
  and a well-named section is what makes a narrow, self-expiring gear shift possible
- Do **not** try to encode the gear in `tasks.md`; it is frozen by `.tasks-approved`, and the gear
  lives in the untracked `.current-gear` file so it can still be shifted afterwards

### Git Workflow
1. Create feature branch
2. Commit first ADR: `git commit -m "docs: add ADR for [decision]"`
3. Create draft PR with ADRs for review
4. Implement incrementally with TDD
