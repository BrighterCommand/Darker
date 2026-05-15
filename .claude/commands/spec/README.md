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
 Tasks.md -----------------> /spec:tasks
      |                      /spec:approve tasks
      |
      v
 Implementation -----------> /spec:implement
      |                      (TDD: Tests -> Code)
      |
      +-- OR (unattended) -> /spec:ralph-tasks
      |                      /spec:ralph-implement [count]
      |                      scripts/ralph.sh [n] [max] [cooldown]
      |
      v
 Pull Request
```

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

Create implementation task list based on approved design.

```bash
/spec:tasks
```

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

Begin TDD implementation of approved specification.

```bash
/spec:implement        # All tasks
/spec:implement 3      # Specific task
```

---

### `/spec:ralph-tasks`

Generate `ralph-tasks.md` for unattended TDD implementation.

```bash
/spec:ralph-tasks
```

---

### `/spec:ralph-implement [count]`

Unattended TDD implementation from `ralph-tasks.md`.

```bash
/spec:ralph-implement       # Next task
/spec:ralph-implement 3     # Next 3 tasks
```

---

### Using the Ralph Loop

```bash
# 1. Complete the spec workflow up to approved tasks
# 2. Generate ralph-tasks
/spec:ralph-tasks

# 3. Review ralph-tasks.md in your IDE

# 4. Run the loop
./scripts/ralph.sh              # defaults: 1 task/run, 50 max iterations
./scripts/ralph.sh 2 20 10      # 2 tasks/run, 20 max, 10s cooldown

# 5. Stop the loop (if needed)
touch RALPH_STOP
```

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
│       ├── tasks.md                       # Implementation tasks
│       ├── ralph-tasks.md                 # Unattended TDD tasks (optional)
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
