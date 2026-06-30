# Specification-Driven Development Commands

This directory contains Claude Code commands that implement a specification-driven development workflow for Darker contributions. These commands help you follow Darker's preferred contribution workflow: **Issue -> Requirements -> ADR(s) -> Tasks -> Tests -> Code**.

## Overview

The spec commands provide a structured approach to designing and implementing features:

1. **Requirements**: Capture user needs and problem statements
2. **Design (ADRs)**: Document architectural decisions (can have multiple ADRs per requirement)
3. **Tasks**: Break down implementation into actionable steps
4. **Implementation**: Follow TDD to write tests and code

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
 Choose by certainty -----> (prompted at /spec:approve design)
      |
      +-- Attended --------> /spec:tasks
      |   (review each       /spec:approve tasks
      |    test)             /spec:implement   (sonnet; TDD: Tests -> Code)
      |
      +-- Unattended ------> /spec:ralph-tasks       (standalone, from approved design)
          (review in         /spec:ralph-implement   (opus + auto mode, self-driving loop)
           batches)
      |
      v
 Pull Request
```

**The certainty fork.** After the design is approved, you pick *one* of two paths — and
`/spec:approve design` prompts you to choose:

- **Attended** (`/spec:tasks` → `/spec:implement`): a strict Red → **user approval** → Green
  → Refactor loop in the main agent on **sonnet**. Every test is reviewed in the IDE before
  implementation. Use when the work is uncertain.
- **Unattended** (`/spec:ralph-tasks` → `/spec:ralph-implement`): `ralph-tasks.md` is
  generated **directly from the approved design** (no `tasks.md`, no per-test gates), then a
  self-driving loop on **opus** under **auto mode** delegates each task to a **sonnet**
  sub-agent. Reviewed in batches rather than per test. Use when the work is well-understood.

## Sub-agents & model policy

Some commands delegate their reasoning-heavy or implementation-heavy work to a **sub-agent**
(launched via the `Agent` tool) rather than doing it inline. This keeps the main conversation's
context clean and gives the heavy work a focused, single-purpose context.

**The convention** (modelled on `/spec:review`):

1. **The main agent gathers inputs.** A sub-agent starts with a clean context — it only knows
   what is in its prompt. The command reads the needed files (and runs `gh`/`git`) first, then
   passes the text or paths.
2. **Launch `Agent`** with an explicit `subagent_type` and `model`:
   - **`/spec:ralph-tasks`** uses `subagent_type: "Plan"`. `Plan` has all tools **except**
     `Agent`, `ExitPlanMode`, `Edit`, `Write`, and `NotebookEdit` — so it can
     Read/Glob/Grep/Bash but has no file-editing tool. That makes it much **harder** for the
     sub-agent to accidentally write the spec file than relying on the prompt alone (it still
     has `Bash`, so the prompt also forbids `echo >`/`tee`/`sed -i`).
   - **`/spec:review`** uses `subagent_type: "general-purpose"` (adversarial reasoning that
     needs no source mutation).
   - **`/spec:ralph-implement`** uses `subagent_type: "general-purpose"` because its per-task
     sub-agent genuinely *writes* source files.
3. **The main agent owns all user interaction.** A sub-agent is one-shot — once launched it
   runs to completion and returns; it cannot pause to ask the user anything. So before
   launching, the main agent clarifies any ambiguous inputs with the user via `AskUserQuestion`,
   then launches the sub-agent with the clarified inputs folded in. The `Plan`-based
   `ralph-tasks` sub-agent has no `AskUserQuestion` so it *structurally* can't prompt; the
   `general-purpose` `review` sub-agent is explicitly instructed not to. **Exception:**
   `/spec:ralph-implement` runs fully **unattended** — neither its orchestrator nor its
   sub-agent prompts the user once the loop starts.
4. **The sub-agent RETURNS its artifact as text** — it does *not* write the spec file. The one
   exception is `/spec:ralph-implement`, whose sub-agent must write the test and implementation
   source files (it still never commits or edits the task list).
5. **The main agent validates** the returned output against a checklist, then writes the file
   and does all bookkeeping (approval markers, `.adr-list`, git, next-steps).

The remaining planning commands (`/spec:requirements`, `/spec:design`, `/spec:tasks`) currently
run inline in the main agent rather than delegating — they have no sub-agent to assign a model to.

**Model policy** — reasoning vs. implementation:

| Command | Sub-agent (type) | Model | Rationale |
|---------|------------------|-------|-----------|
| `/spec:review` | Yes — `general-purpose` | **opus** | Adversarial reasoning |
| `/spec:ralph-tasks` | Yes — `Plan` (read-only) | **opus** | Planning / decomposition |
| `/spec:ralph-implement` (orchestrator) | — (the loop itself) | **opus** | Cheap bookkeeping + **required for auto mode** |
| `/spec:ralph-implement` (per-task sub-agent) | Yes — `general-purpose` (writes source) | **sonnet** | Mechanical TDD implementation, kept off the opus loop context for cost |
| `/spec:implement` | No | **sonnet** (Step 0 prompts to switch) | Implementation work; runs in the main agent, so set the session model |
| `/spec:requirements`, `/spec:design`, `/spec:tasks` | No (main agent) | — | Run inline |
| `/spec:new`, `/spec:switch`, `/spec:approve`, `/spec:status` | No | — | Mechanical bookkeeping |

`/spec:ralph-implement` runs **two models on purpose**: the orchestrator loop on **opus**
(required for **auto mode**, and the policy for the unattended path) does only cheap
bookkeeping — STOP-file check, task selection, marking checkboxes, committing, counting — while
each task's actual test + implementation is delegated to a **sonnet** sub-agent. That keeps the
expensive per-task churn on the cheaper model and out of the opus loop's context, lowering cost
without taking the orchestrator off opus.

`/spec:implement` is deliberately **not** delegated: its per-behavior
Red → user-approval → Green → Refactor loop is interactive, and the mandatory approval gate must
run in the main agent where it can reach the user. Because there is no sub-agent to assign a model
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

---

### `/spec:approve <phase> [adr-number]`

Approve a specification phase or specific ADR.

```bash
/spec:approve requirements
/spec:approve design          # Approve all ADRs
/spec:approve design 0043     # Approve specific ADR
/spec:approve tasks
```

Approving the **design** also **prompts you to choose the implementation path** (the certainty
fork): the **attended** path (`/spec:tasks` → `/spec:implement`, review each test) or the
**unattended** path (`/spec:ralph-tasks` → `/spec:ralph-implement`, review in batches). Either
path can start straight from the approved design.

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

Create implementation task list based on approved design (the **attended** path).

```bash
/spec:tasks
```

> Pick *either* `/spec:tasks` (attended) *or* `/spec:ralph-tasks` (unattended) after design
> approval — they are alternative branches, not sequential steps.

---

### `/spec:status`

Show status of all specifications.

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

### `/spec:implement [task-number]`

Begin TDD implementation of approved specification (the **attended** path). Recommended on
**sonnet**; Step 0 prompts you to switch if the session is on another model.

```bash
/spec:implement        # All tasks
/spec:implement 3      # Specific task
```

**Requirements:** Tasks approved (`.tasks-approved`) and all ADRs Accepted.

Follows a strict Red → **user approval** → Green → Refactor cycle, committing after each
behavior. The approval gate before each implementation is mandatory.

---

### `/spec:ralph-tasks`

Generate `ralph-tasks.md` for unattended TDD implementation. **Standalone** — the unattended
peer of `/spec:tasks`, derived **directly from the approved design** (requirements + ADRs). It
does **not** require `tasks.md` or `.tasks-approved`.

```bash
/spec:ralph-tasks
```

Creates `specs/{current-spec}/ralph-tasks.md`, formatted for unattended execution:

- **No approval gates**: No `STOP HERE` or `/test-first` directives
- **RALPH-VERIFY**: Each task includes an exact `dotnet test --filter` command
- **References**: Each task lists files/ADRs to read (self-contained for fresh context)
- **Strict atomicity**: One behavior per task, ~200 lines max, ordered by dependency

**Requirements:** Design approved (`.design-approved`) and all ADRs Accepted.

**Ralph task format:**
```markdown
- [ ] **[Brief behavior description]**
  - **Behavior**: [Precise behavioral specification]
  - **Test file**: `test/[Project]/[When_condition_should_behavior.cs]`
  - **Test should verify**:
    - [Point 1]
    - [Point 2]
  - **Implementation files**:
    - `src/[Project]/[File.cs]` - [What to add/change]
  - **RALPH-VERIFY**: `dotnet test test/[Project]/ --filter "FullyQualifiedName~When_condition_should_behavior"`
  - **References**: [ADR numbers, requirement sections, existing code files]
```

---

### `/spec:ralph-implement [count]`

Unattended TDD implementation from `ralph-tasks.md` via a **self-driving loop**. Run it on
**opus** with **auto mode** enabled for a true unattended run.

```bash
# Ask the run bound up front, then loop unattended
/spec:ralph-implement

# Shortcut: pre-set the tasks bound to 3 (skips the bound prompt)
/spec:ralph-implement 3
```

**Up-front setup (Step 0, the only interactive part):**
- Advises that the orchestrator should be on **opus** and that **auto mode** should be on
  (auto mode is a permission mode set in Claude Code settings / `CLAUDE_CODE_ENABLE_AUTO_MODE`,
  Opus-gated — the command can't toggle it; it advises and proceeds).
- Asks (via `AskUserQuestion`) which **run bound** to use — *unless* a `count` was passed,
  which sets the tasks bound directly:
  - **Tasks** — stop after N tasks complete (the `count` argument)
  - **Turns** — stop after N loop iterations attempted (failures included)
  - **Budget** — stop after ~N output tokens consumed

**Two models on purpose:** the **opus** orchestrator does only bookkeeping; each task's test +
implementation is delegated to a **sonnet** sub-agent (cheaper, and kept off the opus loop
context). The sub-agent never commits, pushes, or edits the task files.

**Loop per task:** check `RALPH_STOP` → select next `- [ ]` task → delegate 🔴 Red → 🟢 Green
→ 🔵 Refactor to a sonnet sub-agent → orchestrator marks the checkbox and commits → check
continuation → repeat. Long runs can self-pace across context windows with `ScheduleWakeup`.

**Stop mechanisms:**
- The chosen **bound** (tasks / turns / budget)
- `RALPH_STOP` file at repo root — the unattended kill-switch (`touch RALPH_STOP` from another
  terminal); halts after the current task
- **Esc** — cancel a pending self-paced wake-up at the keyboard
- Automatically stops when all tasks complete

**Error handling:** Failed tasks are marked `- [!]` with an explanation and skipped.

**Requirements:**
- Design approved (`.design-approved`)
- `ralph-tasks.md` exists (run `/spec:ralph-tasks` first)
- Recommended: session on **opus** with **auto mode** enabled

---

### Running the unattended loop

The loop runs **in-session** — no external bash runner. Built-in **auto mode** removes the
per-action permission prompts and the self-driving loop (optionally self-paced with
`ScheduleWakeup`) replaces an overnight runner.

```bash
# 1. Spec workflow up to an APPROVED DESIGN (no tasks step needed for this path)
/spec:requirements 123
/spec:approve requirements
/spec:design query-caching
/spec:approve design          # <- prompts you to pick the unattended path

# 2. Generate ralph-tasks directly from the approved design
/spec:ralph-tasks

# 3. Review ralph-tasks.md in your IDE

# 4. Switch to opus and enable auto mode, then run the loop
/model opus
/spec:ralph-implement         # choose tasks / turns / budget when prompted

# 5. Stop the loop (if needed)
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
│       ├── .tasks-approved                # Approval marker (attended path only)
│       ├── .adr-list                      # List of associated ADRs
│       ├── requirements.md                # User requirements
│       ├── tasks.md                       # Implementation tasks (attended path)
│       ├── ralph-tasks.md                 # Unattended TDD tasks (unattended path)
│       └── README.md                      # Spec overview
├── docs/
│   └── adr/
│       ├── 0001-record-architecture-decisions.md
│       ├── 0002-feature-aspect-one.md
│       └── 0003-feature-aspect-two.md
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

### Git Workflow
1. Create feature branch
2. Commit first ADR: `git commit -m "docs: add ADR for [decision]"`
3. Create draft PR with ADRs for review
4. Implement incrementally with TDD
