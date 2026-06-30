# Claude Code Skills Overview

This document provides a quick reference for the Claude Code skills available for Darker development.

## What Are Skills?

Skills are slash commands that automate multi-step workflows and enforce Darker's engineering practices. Instead of manually following documented procedures, you invoke a skill that guides you through the process.

## Available Skills

### Core Development Skills

| Skill | Purpose | Usage |
|-------|---------|-------|
| `/test-first` | TDD with mandatory approval | `/test-first <behavior description>` |
| `/tidy-first` | Separate refactoring from features | `/tidy-first <change description>` |
| `/adr` | Create Architecture Decision Record | `/adr <title>` |
| `/bugfix:*` | Diagnosis-first bug workflow (Confirm gate) | `/bugfix:triage [issue \| description]` |

### Bugfix Workflow Skills

| Skill | Purpose | Usage |
|-------|---------|-------|
| `/bugfix:triage` | Restate symptom, locate code, form hypothesis | `/bugfix:triage [issue-number \| description]` |
| `/bugfix:confirm` | ✋ Prove the root cause before any fix | `/bugfix:confirm` |
| `/bugfix:test` | Failing regression test (via `/test-first`) | `/bugfix:test` |
| `/bugfix:fix` | Minimal fix scoped to the confirmed cause | `/bugfix:fix` |
| `/bugfix:verify` | Run suite; capture root cause + `Fixes #N` | `/bugfix:verify` |
| `/bugfix:status` | Show all bugs and their phase | `/bugfix:status` |
| `/bugfix:switch` | Switch the active bug | `/bugfix:switch <NNNN-slug>` |

### Specification Workflow Skills

| Skill | Purpose | Usage |
|-------|---------|-------|
| `/spec:requirements` | Capture requirements | `/spec:requirements [issue-number]` |
| `/spec:design` | Create design ADRs | `/spec:design <focus-area>` |
| `/spec:tasks` | Break down implementation | `/spec:tasks` |
| `/spec:implement` | TDD implementation | `/spec:implement [task-number]` |
| `/spec:status` | Show spec status | `/spec:status` |
| `/spec:approve` | Approve phases | `/spec:approve <phase> [adr-number]` |
| `/spec:review` | Review phases | `/spec:review [phase] [adr-number]` |
| `/spec:switch` | Switch to different spec | `/spec:switch <spec-name>` |
| `/spec:ralph-tasks` | Generate unattended TDD tasks | `/spec:ralph-tasks` |
| `/spec:ralph-implement` | Unattended TDD implementation | `/spec:ralph-implement [count]` |

## Quick Reference Card

```
┌────────────────────────────────────────────────────────────────┐
│                    CLAUDE CODE SKILLS                          │
│                    Quick Reference                              │
└────────────────────────────────────────────────────────────────┘

BUGFIX (DIAGNOSIS-FIRST)
   /bugfix:triage -> :confirm -> :test -> :fix -> :verify
   Triage -> Confirm gate -> Test-first -> Fix -> Verify
   Proves the root cause BEFORE writing a fix
   Example: /bugfix:triage 123

TEST-DRIVEN DEVELOPMENT
   /test-first <behavior>
   Write test -> Approve -> Implement -> Refactor
   Enforces mandatory approval before implementation
   Example: /test-first when query handler throws it should invoke fallback policy

REFACTORING
   /tidy-first <change>
   Separate structural changes from behavioral changes
   Creates two commits: refactor + feat/fix/perf
   Example: /tidy-first optimize query pipeline building

ARCHITECTURE DECISIONS
   /adr <title>
   Auto-numbered, properly formatted ADRs
   Links to current spec if applicable
   Example: /adr query caching strategy

SPECIFICATION WORKFLOW
   /spec:requirements [issue]    -> Capture requirements
   /spec:design <focus>          -> Create design ADR
   /spec:tasks                   -> Break down work
   /spec:implement [task]        -> TDD implementation
   /spec:status                  -> Show all specs
   /spec:approve <phase>         -> Approve phase

RALPH LOOP (UNATTENDED)
   /spec:ralph-tasks              -> Generate ralph tasks (standalone, from approved design)
   /spec:ralph-implement [count]  -> Unattended self-driving loop (opus + auto mode)
```

## Decision Tree: Which Skill Should I Use?

```
                    Need to document decision?
                              |
                    +----------+----------+
                   YES                 NO
                    |                   |
                 /adr            Fixing a bug?
                                       |
                          +------------+------------+
                        YES                       NO
                          |                        |
                 Root cause proven?         Adding behavior?
                          |                        |
                  +-------+-------+         +-------+-------+
                NO              YES        YES             NO
                 |               |          |               |
          /bugfix:triage    /test-first  Code needs    Just refactoring
          (Confirm gate)                 cleanup?      -> /tidy-first
                                             |
                                     +-------+-------+
                                   YES             NO
                                     |              |
                               /tidy-first    /test-first
```

## Enforcement of Darker Practices

Each skill enforces specific practices from `.agent_instructions/`:

### `/test-first` -> testing.md

**Enforces**:
- Red-Green-Refactor TDD cycle
- **MANDATORY approval before implementation**
- BDD-style test naming (`When_X_should_Y`)
- One test per file
- Developer tests (not unit tests)

**Reference**: [testing.md](testing.md)

### `/adr` -> documentation.md

**Enforces**:
- Proper ADR numbering sequence
- Standard ADR template structure
- Dash-case file naming
- Linking to parent requirements
- Tracking in spec's `.adr-list`

**Reference**: [documentation.md](documentation.md)

### `/tidy-first` -> code_style.md

**Enforces**:
- Separation of structural from behavioral changes
- **Never mix in same commit**
- Structural changes FIRST
- Test validation after structural changes
- Two distinct commits for clarity

**Reference**: [code_style.md](code_style.md)

## Common Workflows

### Workflow 0: Fix a Bug (Diagnosis-First)

```bash
# 1. Triage — restate symptom, locate code, form hypothesis (suggested fix is UNVERIFIED)
/bugfix:triage 123

# 2. ✋ Confirm — prove the root cause BEFORE any fix (may widen the scope)
/bugfix:confirm

# 3. ✋ Test-first — failing regression test (delegates to /test-first)
/bugfix:test

# 4. Fix — minimal change to green, scoped to the confirmed cause
/bugfix:fix

# 5. Verify — run the suite; commit captures root cause + Fixes #123
/bugfix:verify
```

### Workflow 1: Add New Feature (Clean Code)

```bash
# 1. Write test first
/test-first when query handler returns null it should throw QueryHandlerNotFoundException

# 2. Repeat for each behavior
/test-first when decorator attribute has higher step it should execute first

# 3. Document decision if significant
/adr decorator ordering strategy
```

### Workflow 2: Add Feature (Code Needs Cleanup)

```bash
# 1. Clean up and add feature in one workflow
/tidy-first add caching to query pipeline

# Output: Two commits
#   - refactor: simplify pipeline builder structure
#   - feat: add query result caching to pipeline
```

### Workflow 3: Specification-Driven Development

```bash
# 1. Create spec from GitHub issue
/spec:requirements 123

# 2. Review and approve requirements
/spec:approve requirements

# 3. Create design ADRs (uses /adr internally)
/spec:design query-caching
/spec:design decorator-pipeline
/spec:approve design

# 4. Break down into tasks
/spec:tasks
/spec:approve tasks

# 5. Implement (uses /test-first approach)
/spec:implement

# 6. Check progress
/spec:status
```

### Workflow 4: Pure Refactoring

```bash
# Refactor without adding features
/tidy-first simplify nested conditionals in PipelineBuilder

# Output: Single commit
#   - refactor: simplify nested conditionals in PipelineBuilder
```

### Workflow 5: Ralph Loop (Unattended)

```bash
# 1. Complete spec workflow up to an APPROVED DESIGN (no tasks step needed for this path)
#    /spec:requirements -> /spec:approve requirements -> /spec:design -> /spec:approve design

# 2. Generate ralph-tasks directly from the approved design
/spec:ralph-tasks

# 3. Review ralph-tasks.md in your IDE

# 4. Switch to opus + enable auto mode, then run the self-driving loop
/model opus
/spec:ralph-implement          # choose the bound: tasks / turns / budget

# 5. Stop if needed
touch RALPH_STOP               # unattended kill-switch (or press Esc for a pending wake-up)

# 6. Review results
git log --oneline
```

## Detailed Documentation

- **Skills Overview**: [.claude/commands/README.md](../../.claude/commands/README.md)
- **Test-First**: [.claude/commands/tdd/README.md](../../.claude/commands/tdd/README.md)
- **ADR**: [.claude/commands/adr/README.md](../../.claude/commands/adr/README.md)
- **Tidy First**: [.claude/commands/refactor/README.md](../../.claude/commands/refactor/README.md)
- **Bugfix Workflow**: [.claude/commands/bugfix/README.md](../../.claude/commands/bugfix/README.md)
- **Spec Workflow**: [.claude/commands/spec/README.md](../../.claude/commands/spec/README.md)

---

**Remember**: Skills make the **correct approach the easy path**. Instead of remembering procedures, just invoke the skill and it guides you through.
