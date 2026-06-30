---
allowed-tools: Bash(cat:*), Bash(test:*), Bash(ls:*), Bash(echo:*), Bash(dotnet:*), Bash(git:*), Read, Write, Edit, Glob, Grep, AskUserQuestion
description: Start TDD implementation from approved tasks
argument-hint: [task-number]
---

## Context

Current spec directory: specs/

**Workflow**: Issue -> Requirements -> ADR(s) -> Tasks -> **Tests -> Code**

**TDD Cycle**: RED -> User Approval -> GREEN -> REFACTOR

> **Recommended model: `sonnet`.** Unlike the unattended `/spec:ralph-implement`,
> `/spec:implement` does its work in the **main agent** (the interactive approval gate must
> reach the user), so there is no sub-agent to assign a model to — the session model is what
> runs. This is implementation work, which the model policy puts on **sonnet**. Step 0 below
> actively prompts you to switch if the session is on another model. See
> `.claude/commands/spec/README.md` → "Sub-agents & model policy".

## Critical Guidelines

**ALWAYS follow these instructions when writing code:**
- **Testing**: [.agent_instructions/testing.md](../../../.agent_instructions/testing.md)
- **Code Style**: [.agent_instructions/code_style.md](../../../.agent_instructions/code_style.md)

## Your Task

### Step 0: Confirm the Session Model

`/spec:implement` is interactive implementation work and the model policy puts it on
**sonnet**. Check the session's current model:

- **If already on sonnet**: continue silently to Step 1.
- **If on any other model** (e.g. opus, haiku): use `AskUserQuestion` to ask whether to
  switch to sonnet before starting — e.g. "This session is on {model}. `/spec:implement` is
  recommended on sonnet. Switch to sonnet first?" with options to **switch** (tell the user to
  run `/model sonnet`, since a command can't change the session model itself) or **continue on
  the current model**. Respect the choice; do not switch on their behalf, and do not block if
  they decline.

This is a one-time check at the start of the command.

### Step 1: Gather Context

1. Read `specs/.current-spec` to determine the active specification directory
2. Verify `.tasks-approved` exists in that directory
3. Read `specs/{current-spec}/tasks.md` to see task list
4. Read `specs/{current-spec}/.adr-list` to see all ADRs
5. Read ADRs from `docs/adr/` to understand design decisions
6. If task number provided in $ARGUMENTS, focus on that task only

### Step 2: Verify Prerequisites

Check that all phases are approved:
- Requirements: `.requirements-approved` exists
- Design: `.design-approved` exists and all ADRs have Status "Accepted"
- Tasks: `.tasks-approved` exists

If not all approved, inform user and exit.

### Step 3: Select Task

Display current incomplete tasks from tasks.md.

If task number provided, work on that specific task.
Otherwise, suggest the next logical task to work on.

### Step 4: TDD Implementation Cycle

For each task, follow this strict workflow:

#### RED: Write a Failing Test

1. **Read Testing Guidelines**: Review [.agent_instructions/testing.md](../../../.agent_instructions/testing.md)

2. **Understand the Behavior**: Identify the specific behavior this task requires

3. **Write the Test** following these rules from testing.md:
   - **Test naming**: `When_[condition]_should_[expected_behavior]`
   - **File naming**: Prefer one test case per file named `When_[condition]_should_[expected_behavior].cs`
   - **Structure**: Use Arrange/Act/Assert with explicit comments
   - **Evident Data**: Highlight the state that impacts the test outcome
   - **Test behavior, not implementation**: Test public exports only
   - **No mocks for isolation**: Use developer tests that implicate the most recent edit
   - **Only test public exports**: Don't test private or internal methods

4. **Create/Update Test File**: Use Write or Edit tool to create the test

5. **Run the Test**: Use Bash to run: `dotnet test test/Paramore.Darker.Tests/ --filter "FullyQualifiedName~When_[test_name]"`
   - Verify the test FAILS (Red)

6. **Show Test to User**

#### USER APPROVAL: Get Approval for Test

**CRITICAL**: Before writing any implementation code, you MUST:

1. Use AskUserQuestion tool to ask: "I've written a failing test for [behavior]. Should I proceed to make this test pass?"

2. Wait for user approval

3. If user requests changes to the test:
   - Make the requested changes
   - Re-run the test to verify it still fails correctly
   - Ask for approval again

**DO NOT proceed to implementation without explicit user approval of the test.**

#### GREEN: Make the Test Pass

1. **Read Code Style Guidelines**: Review [.agent_instructions/code_style.md](../../../.agent_instructions/code_style.md)

2. **Write Minimum Code** to make the test pass:
   - Only write code necessary for the test to pass
   - No speculative code

3. **Follow Code Style** from code_style.md:
   - Use .NET C# naming conventions (PascalCase for public, camelCase for private)
   - Use ALL_CAPS for constants with underscores
   - Expression-bodied members for simple properties/methods
   - readonly for fields that don't change after construction
   - Enable nullable reference types
   - Responsibility Driven Design principles
   - Avoid primitive obsession

4. **Run the Test Again**: Verify the test PASSES (Green)

5. **Run All Tests**: `dotnet test Darker.Filter.slnf` to ensure no regressions

#### REFACTOR: Improve the Design

1. **Review the Code** for design improvements
2. **Apply "Tidy First" Principles**: Structural changes only, no behavioral changes
3. **Run All Tests After Each Refactoring**: Verify no behavioral changes

### Step 5: Commit the Change

After completing Red-Green-Refactor for a behavior:

1. **Stage Changes**: `git add [test-file] [implementation-files]`
2. **Commit with Descriptive Message**
3. **Update Tasks**: Use Edit tool to check off completed task in `specs/{current-spec}/tasks.md`

### Step 6: Continue to Next Behavior

Ask user: "This behavior is complete. Should I continue to the next test, or would you like to review?"

## Important Reminders

### Test-First Requirements

- **NEVER write implementation before writing a failing test**
- **ALWAYS get user approval of the test before implementing**
- Each test should represent the smallest possible behavioral step

### Code Quality Requirements

- Follow ALL guidelines in .agent_instructions/testing.md
- Follow ALL guidelines in .agent_instructions/code_style.md
- Keep changes small and incremental
- Commit frequently (after each successful cycle)

### Test Scope

- Only test public exports from assemblies
- Don't test private or internal implementation details
- Tests should be coupled to behavior, not implementation

### Design Principles

- Responsibility Driven Design: Focus on "knowing", "doing", "deciding"
- Distribute behavior: Make objects smart
- Preserve flexibility: Interior details should be changeable
- Avoid primitive obsession: Use expressive types
- Keep methods small: Single responsibility, minimal indentation

Use Read, Write, Edit, Bash, and AskUserQuestion tools throughout the implementation process.
