# Refactoring Commands

This directory contains Claude Code commands for safe, disciplined refactoring following Darker's code style principles.

## Commands

### `/refactor:tidy-first <description of desired change>`

Implements Kent Beck's "Tidy First" methodology by separating structural changes from behavioral changes into distinct commits.

**Purpose**: Ensures refactoring (structural changes) are separated from functionality changes (behavioral changes), making code reviews easier, git history clearer, and reducing bugs.

**Usage:**
```bash
/refactor:tidy-first optimize the pipeline building in QueryProcessor
```

### `/refactor:smells [base-ref | base..head | path...]`

An opt-in review, run after a unit of work completes (a task, a spec phase, or a branch) and its tests
are green, that looks for Martin Fowler's code smells (*Refactoring*, 2nd ed.) in the changed code,
calibrated against Darker's design principles and code style.

**Purpose**: Turn "this could be tidier" into scheduled, named refactorings. It is most useful after a
stretch worked in the `review-after` gear, where no human read each change as it landed. Each finding
gives the smell, its location and evidence, and the refactoring that removes it, and is marked
`structural` (a `/refactor:tidy-first` job) or `public API change` (needs a decision first).

**It reports and schedules; it does not refactor.** Findings become a `/refactor:tidy-first` run, or `STRUCTURAL`
tasks appended to the active spec's `tasks.md`, as you choose. Code copied verbatim from Brighter is
reported only as "worth a look", since a fix belongs upstream.

**Usage:**
```bash
/refactor:smells                 # current branch vs master, plus uncommitted changes
/refactor:smells HEAD~3          # changes since a ref
/refactor:smells abc123^1..abc123  # changes between two refs, e.g. a merged PR's merge commit
/refactor:smells src/Paramore.Darker/PipelineBuilder.cs
```

## The Core Principle

From [.agent_instructions/code_style.md](../../../.agent_instructions/code_style.md):

> **Never mix structural and behavioral changes in the same commit**

**Structural Changes** (refactoring):
- Renaming for clarity
- Extracting methods
- Reducing nesting/indentation
- Moving code between files
- Converting primitives to expressive types
- Simplifying conditionals (same logic, clearer structure)

**Behavioral Changes** (functionality):
- Adding features
- Changing algorithms
- Modifying error handling
- Adding validation
- Changing I/O or caching
- Modifying business logic

## Workflow

The `/refactor:tidy-first` command guides you through 9 phases:

1. **Analysis** - Categorizes changes into structural vs behavioral
2. **Plan** - Gets your approval of categorization
3. **Structural Changes** - Makes refactoring changes WITHOUT altering behavior
4. **Validate Structural** - Runs tests to prove behavior unchanged
5. **Commit Structural** - Creates `refactor:` commit
6. **Behavioral Changes** - Makes functionality changes
7. **Validate Behavioral** - Runs tests with new behavior
8. **Commit Behavioral** - Creates `feat:`/`fix:`/`perf:` commit
9. **Summary** - Shows both commits and suggests next steps

## When to Use Tidy First

**Good Fit:**
- Need to refactor code AND add/change functionality
- Existing code is hard to understand before adding feature
- Changes touch messy code that needs cleanup

**Not a Good Fit:**
- Pure refactoring with no functionality changes (just do it and commit as `refactor:`)
- Pure feature addition (use `/test-first` instead for TDD)
- Trivial changes that don't need structural cleanup

## Related Commands

- **`/test-first`** - TDD workflow for pure feature additions
- **`/spec:implement`** - Specification-driven implementation (uses TDD)
- **`/adr`** - Document significant refactoring decisions

## Related Documentation

- [Code Style Guide](../../../.agent_instructions/code_style.md)
- [Testing Guidelines](../../../.agent_instructions/testing.md)
- [Kent Beck's Tidy First Book](https://www.oreilly.com/library/view/tidy-first/9781098151232/)
