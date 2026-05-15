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
   /spec:ralph-tasks              -> Generate ralph tasks
   /spec:ralph-implement [count]  -> Unattended TDD
   scripts/ralph.sh [n] [max]     -> Run the loop
```

## Decision Tree: Which Skill Should I Use?

```
                    Need to document decision?
                              |
                    +----------+----------+
                   YES                 NO
                    |                   |
                 /adr                   |
                                Adding behavior?
                                       |
                            +-----------+-----------+
                          YES                    NO
                            |                     |
                            |                     |
                  Code needs cleanup?      Just refactoring?
                            |                     |
                    +--------+--------+            |
                  YES              NO             |
                    |               |             |
              /tidy-first     /test-first   /tidy-first
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
# 1. Complete spec workflow up to approved tasks (Workflows 3 steps 1-4)

# 2. Generate ralph-tasks from approved tasks
/spec:ralph-tasks

# 3. Review ralph-tasks.md in your IDE

# 4. Run the unattended loop
./scripts/ralph.sh              # 1 task/run, 50 max iterations
./scripts/ralph.sh 2 20 10      # 2 tasks/run, 20 max, 10s cooldown

# 5. Stop if needed
touch RALPH_STOP

# 6. Review results
git log --oneline
```

## Detailed Documentation

- **Skills Overview**: [.claude/commands/README.md](../../.claude/commands/README.md)
- **Test-First**: [.claude/commands/tdd/README.md](../../.claude/commands/tdd/README.md)
- **ADR**: [.claude/commands/adr/README.md](../../.claude/commands/adr/README.md)
- **Tidy First**: [.claude/commands/refactor/README.md](../../.claude/commands/refactor/README.md)
- **Spec Workflow**: [.claude/commands/spec/README.md](../../.claude/commands/spec/README.md)

---

**Remember**: Skills make the **correct approach the easy path**. Instead of remembering procedures, just invoke the skill and it guides you through.
