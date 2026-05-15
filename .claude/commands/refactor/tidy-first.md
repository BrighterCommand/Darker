---
allowed-tools: Read, Write, Edit, Glob, Grep, Bash, AskUserQuestion, TodoWrite
description: Separate structural and behavioral changes following Beck's Tidy First
argument-hint: <description of desired change>
---

# Tidy First - Separate Structural and Behavioral Changes

You are guiding the user through Beck's "Tidy First" methodology, which requires separating structural changes from behavioral changes into distinct commits.

## The Desired Change

$ARGUMENTS

## Core Principle

From [.agent_instructions/code_style.md](../../../.agent_instructions/code_style.md):

> **Never mix structural and behavioral changes in the same commit**

- **STRUCTURAL CHANGES**: Rearranging code without changing behavior (renaming, extracting methods, moving code)
- **BEHAVIORAL CHANGES**: Adding or modifying actual functionality

This separation makes code reviews easier, reduces bugs, and creates clearer git history.

## Your Task Workflow

### Phase 1: Analysis - Understand the Change

First, understand what the user wants to accomplish and identify the files involved.

**Steps:**
1. Read the relevant code files to understand current state
2. Analyze what needs to change to accomplish the goal
3. Categorize changes into STRUCTURAL vs BEHAVIORAL

Use TodoWrite to track the changes you identify:
```
- Analyze current code (in_progress)
- Identify structural changes needed (pending)
- Identify behavioral changes needed (pending)
- Make structural changes (pending)
- Validate with tests (pending)
- Commit structural changes (pending)
- Make behavioral changes (pending)
- Validate with tests (pending)
- Commit behavioral changes (pending)
```

**Categorization Guide:**

**STRUCTURAL (refactoring) - Does NOT change behavior:**
- Renaming variables, methods, classes
- Extracting methods to reduce complexity
- Moving code to different files/classes
- Simplifying nested conditionals (same logic, clearer structure)
- Reducing indentation levels
- Breaking up large methods
- Replacing magic numbers with named constants
- Converting primitives to expressive types (if behavior identical)

**BEHAVIORAL - DOES change functionality:**
- Adding new features
- Changing algorithms or logic
- Modifying error handling
- Adding validation
- Adding caching
- Modifying retry/policy logic
- Changing data transformations

### Phase 2: Plan - Present Analysis to User

Use AskUserQuestion to confirm your analysis:

**Question**: "I've analyzed the changes needed. Does this categorization look correct?"

**Show the user:**
```
STRUCTURAL changes (to be done first):
- [List structural changes you identified]

BEHAVIORAL changes (to be done after):
- [List behavioral changes you identified]
```

**Options:**
1. "Yes, proceed with structural changes first" - Continue to Phase 3
2. "Adjust the categorization" - User explains adjustments needed
3. "Skip structural changes, do behavioral only" - Jump to Phase 5

### Phase 3: Structural Changes - Refactor Without Changing Behavior

**CRITICAL RULE**: Do NOT change behavior in this phase. Only change structure.

**Steps:**
1. Make the structural changes you identified
2. Follow code style guidelines from [.agent_instructions/code_style.md](../../../.agent_instructions/code_style.md)
3. Update XML documentation if public API structure changed
4. Do NOT add new functionality
5. Do NOT change logic or algorithms

### Phase 4: Validate - Ensure No Behavior Changed

**CRITICAL STEP**: Prove that structural changes didn't alter behavior.

```bash
dotnet test Darker.Filter.slnf
```

**If tests fail:**
- This means you accidentally changed behavior
- Revert the breaking change
- Fix it to be truly structural
- Run tests again until all pass

### Phase 5: Commit Structural Changes

**Only commit if all tests pass.**

**Commit Message Format:**
```
refactor: [brief description of structural changes]
```

### Phase 6: Behavioral Changes - Add or Modify Functionality

**Now you can change behavior.**

**If adding significant new behavior:**
- Suggest using `/test-first` command instead
- Tidy First is best for changes with both refactoring and behavior modifications
- Pure feature additions work better with TDD workflow

### Phase 7: Validate - Ensure Changes Work Correctly

```bash
dotnet test Darker.Filter.slnf
```

### Phase 8: Commit Behavioral Changes

**Commit Message Format:**
```
[type]: [brief description of behavioral change]
```

**Commit Types:**
- `feat:` - New feature
- `fix:` - Bug fix
- `perf:` - Performance improvement

### Phase 9: Summary and Review

Show the user:
1. Summary of both commits created
2. Offer to push to remote if on a feature branch
3. Suggest next steps

## Important Notes

### When to Use Tidy First

**Good fit:**
- You need to refactor code AND add/change functionality
- Existing code is hard to understand/modify
- Changes touch multiple areas requiring cleanup

**Not a good fit:**
- Pure refactoring (no behavioral changes planned)
- Pure feature addition (use `/test-first` instead)
- Trivial changes that don't need structural cleanup

## Related Commands

- **`/test-first`** - Use for pure feature addition with TDD
- **`/spec:implement`** - Uses TDD workflow, could benefit from tidy-first approach
