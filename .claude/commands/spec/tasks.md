---
allowed-tools: Bash(cat:*), Bash(grep:*), Bash(test:*), Bash(find:*), Bash(touch:*), Bash(ls:*),  Bash(echo:*), Read, Write, Glob
description: Create implementation task list
---

## Context

Current spec directory: specs/

## Your Task

First, read specs/.current-spec to determine the active specification directory.

1. Verify design is approved (look for .design-approved file in the spec directory)
2. Create tasks.md with:
   - Detailed task list with checkboxes
   - Task dependencies
   - Risk mitigation tasks
3. Each task should be specific and actionable
4. A task MUST represent implementing a behavior and NOT an implementation detail
5. Use markdown checkboxes: `- [ ] Task description`

Organize tasks to enable incremental development and testing.

## CRITICAL: TDD Task Format

**MANDATORY**: When creating TEST tasks, you MUST format them to enforce `/test-first` skill usage:

### Task Template

```markdown
- [ ] **TEST + IMPLEMENT: [Behavior description]**
  - **USE COMMAND**: `/test-first [behavior description for command]`
  - Test location: "[test directory path]"
  - Test file: `[When_condition_should_behavior.cs]`
  - Test should verify:
    - [verification point 1]
    - [verification point 2]
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - [implementation point 1 with specific file/line numbers where applicable]
    - [implementation point 2]
```

### Example Task

```markdown
- [ ] **TEST + IMPLEMENT: QueryProcessor throws when handler not registered**
  - **USE COMMAND**: `/test-first when query handler not registered should throw QueryHandlerNotFoundException`
  - Test location: "test/Paramore.Darker.Tests"
  - Test file: `When_query_handler_not_registered_should_throw_QueryHandlerNotFoundException.cs`
  - Test should verify:
    - QueryProcessor.Execute called with unregistered query type
    - QueryHandlerNotFoundException thrown with descriptive message
    - Exception includes the query type name
  - **⛔ APPROVAL GATE — STOP HERE and WAIT FOR USER APPROVAL in IDE before implementing** *(fires in the `review-before` gear, which is the default)*
  - Implementation should:
    - In QueryProcessor.Execute() check handler registry for query type
    - Throw QueryHandlerNotFoundException if no handler found
    - Include query type name in exception message
```

### Why This Format?

1. **Visible command**: The `/test-first` command is prominently displayed
2. **Stop sign**: The ⛔ and "STOP HERE" make the approval gate unmissable
3. **Single task**: Combines TEST + IMPLEMENT so workflow is clear
4. **Complete context**: All details needed for test and implementation
5. **IDE review**: Explicitly states user will review in IDE, not CLI

### The gate line and the review gear

Write the `⛔` line on **every** behavioral task. It states the default: the gate is armed unless
the spec has been deliberately shifted into the `review-after` gear (see [`gear.md`](gear.md) and
Brighter's [ADR 0071](https://github.com/BrighterCommand/Brighter/blob/master/docs/adr/0071-tdd-review-gear.md)).

Do **not** try to encode the gear in `tasks.md` — no per-task or per-phase "no gate" annotations,
and no omitting the `⛔` line for tasks you expect to run unattended. `tasks.md` is frozen once
`.tasks-approved` lands, so a gear written into it could not be shifted afterwards without lifting
that freeze. The gear lives in the untracked `specs/{spec}/.current-gear` file and is shifted with
`/spec:gear`. The `⛔` line is a statement of the default, not a per-task switch.

Do keep **section headings** meaningful and stable, though: `/spec:gear` can scope a gear to a
single heading, so a well-named section ("Phase 2 — Cache key generation (behaviour)", "Task 6-11 —
Provider coverage") is what makes a narrow, self-expiring gear shift possible. Any heading
convention works — `## Phase N`, `## Task N`, a named group — and the depth does not matter; a scope
can also be a `tasks N-M` range. What does not work is a single flat `## Tasks` heading over forty
tasks, because there is then nothing to scope to short of the whole spec.

### Task-type tag form (required)

Every task checkbox opens its bold lead-in with exactly one tag, followed immediately by a colon,
with any task id after the colon — one template line per tag:

```markdown
- [ ] **TEST + IMPLEMENT: T1.1 — …**
- [ ] **CHARACTERISE: T1.2 — …**
- [ ] **STRUCTURAL: T1.3 — …**
- [ ] **SETUP: T1.4 — …**
- [ ] **DOC: T1.5 — …**
- [ ] **VERIFY: T1.6 — …**
```

The tag must come first, immediately after `**`. `/spec:ralph-implement` dispatches on the leading
label, and skips (marks `- [!]`) any task whose label it does not recognise — see the drifted form
in the *DO NOT* block below.

- **`TEST + IMPLEMENT`** — the normal behavioral task, using the template above.
- **`CHARACTERISE`** — a test for behavior an earlier task may already deliver (common when a spec
  takes one task per acceptance criterion). Use the same template, plus a **named RED mutation**: a
  `🔁 RED mutation:` bullet naming the temporary change to *production* code that must make the
  test fail, and the assertion it must fail on. A green-on-arrival test is then proved able to fail
  without being weakened or rewritten (see `.agent_instructions/testing.md` → *When a new test
  passes on first run*).
- **`STRUCTURAL`** — a Tidy First change: no new test, existing suite green before and after.
- **`SETUP`** — project/config/package scaffolding; no test, but the build must succeed.
- **`DOC`** — documentation only.
- **`VERIFY`** — a checkpoint that runs named checks and should need no source change.

### DO NOT Format Tasks Like This

BAD - Separates test and implementation:
```markdown
- [ ] **TEST: Handler not registered**
  - Write test...
  - **APPROVAL REQUIRED BEFORE IMPLEMENTATION**

- [ ] **IMPLEMENT: Handler not registered behavior**
  - Handle missing handler...
```

This format allows Claude to skip the approval by treating them as independent tasks.

BAD - Puts the task id before the tag:
```markdown
- [ ] **T1.1 — STRUCTURAL: Extract shared helper**
```

This drifts from the tag-first form above. `/spec:ralph-implement` does not recognise it as a
`STRUCTURAL` task and will skip it.

## Next Steps

Remind the user to:
- Review `tasks.md`
- Run `/spec:review tasks` for an adversarial coverage review, then
- `/spec:approve tasks` when ready to begin implementation.

`tasks.md` is the **single** task list, and both drivers run it:
- `/spec:implement` — one task at a time, approval gate armed by default.
- `/spec:ralph-implement` — a self-driving unattended loop over the same list, in the
  `review-after` gear.

The choice between them is a gear change (`/spec:gear`), not a different task list — so do not
draft tasks "for unattended execution". Draft them once, well.
