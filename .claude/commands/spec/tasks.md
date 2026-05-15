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
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
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
  - **STOP HERE - WAIT FOR USER APPROVAL in IDE before implementing**
  - Implementation should:
    - In QueryProcessor.Execute() check handler registry for query type
    - Throw QueryHandlerNotFoundException if no handler found
    - Include query type name in exception message
```

### Why This Format?

1. **Visible command**: The `/test-first` command is prominently displayed
2. **Stop sign**: The STOP HERE makes the approval gate unmissable
3. **Single task**: Combines TEST + IMPLEMENT so workflow is clear
4. **Complete context**: All details needed for test and implementation
5. **IDE review**: Explicitly states user will review in IDE, not CLI

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
