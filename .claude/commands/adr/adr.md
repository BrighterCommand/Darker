---
allowed-tools: Read, Write, Glob, Bash, AskUserQuestion
description: Create Architecture Decision Record following Darker's template
argument-hint: <title-of-decision>
---

# Architecture Decision Record (ADR) Creation

You are helping the user create an Architecture Decision Record following Darker's ADR template and conventions.

## Decision Title

$ARGUMENTS

## Your Task

Create a new Architecture Decision Record in the `docs/adr/` directory following the project's conventions from [.agent_instructions/documentation.md](../../../.agent_instructions/documentation.md).

### Step 1: Determine Next ADR Number

Find the highest numbered ADR file in `docs/adr/` and calculate the next sequence number.

```bash
ls -1 docs/adr/*.md | grep -E '^docs/adr/[0-9]+' | sed 's/docs\/adr\/\([0-9]*\).*/\1/' | sort -n | tail -1
```

If `docs/adr/` does not exist, create it and start with number `0001`.

The next ADR number should be the highest number + 1, formatted as a 4-digit number with leading zeros (e.g., `0037`).

### Step 2: Check for Linked Specification

Check if there's a current specification this ADR should be linked to:

```bash
# Check if specs/.current-spec exists
if [ -f specs/.current-spec ]; then
    cat specs/.current-spec
fi
```

If a current spec exists:
- Read `specs/[spec-name]/requirements.md` to understand context
- This ADR should reference the parent requirement
- Add this ADR to `specs/[spec-name]/.adr-list`

### Step 3: Read the ADR Template

Read the foundational ADR to understand the expected structure:
- `docs/adr/0001-record-architecture-decisions.md` - Basic template

If the first ADR does not exist yet, use the template from Step 5 directly.

### Step 4: Gather Information from User

Use AskUserQuestion to gather the key information needed for the ADR:

**Question 1: What is the architectural problem or decision to be made?**
- This becomes the Context section
- Should describe the situation requiring a decision
- Include constraints, requirements, and any background

**Question 2: What is your proposed solution or decision?**
- This becomes the Decision section
- Describe WHAT you're doing and WHY
- Include implementation details, diagrams, code examples if helpful

**Question 3 (Optional): What are the main alternatives you considered?**
- This becomes the Alternatives Considered section
- List other options and why they were rejected
- Helps future readers understand the decision rationale

**Question 4 (Optional): What are the key consequences (positive and negative)?**
- This becomes the Consequences section
- Positive outcomes
- Negative outcomes
- Risks and mitigations

### Step 5: Create the ADR File

Create the file at: `docs/adr/[NNNN]-[title].md`

**File naming**:
- Use 4-digit sequence number with leading zeros
- Use dash-case (kebab-case) for the title
- Example: `0002-query-caching-strategy.md`

**File structure**:

```markdown
# [Number]. [Title]

Date: [YYYY-MM-DD]

## Status

Proposed

## Context

[If linked to spec]
**Parent Requirement**: [specs/[spec-name]/requirements.md](../../specs/[spec-name]/requirements.md)

**Scope**: [One sentence describing what this ADR covers]

### The Problem

[Describe the architectural problem or decision that needs to be made]

### Requirements Context

[Reference any relevant requirements from the parent spec]

### Constraints

[List any constraints that influence the decision]

## Decision

[Describe what you decided to do and WHY]

### [Sub-sections as needed]

[Break down complex decisions into clear sub-sections]

## Consequences

### Positive

[Good outcomes from this decision]

### Negative

[Drawbacks or costs from this decision]

### Risks and Mitigations

**Risk**: [Description of risk]
- **Mitigation**: [How you'll address it]

## Alternatives Considered

### Alternative 1: [Name]

[Description]

**Rejected because**:
- [Reason 1]
- [Reason 2]

## References

[If linked to spec]
- Requirements: [specs/[spec-name]/requirements.md](../../specs/[spec-name]/requirements.md)
- Related ADRs:
  - [ADR NNNN: Title](NNNN-title.md) - Brief description of relationship
- External references:
  - [Link to external documentation, articles, etc.]
```

### Step 6: Update Spec ADR List (if applicable)

If linked to a spec, append this ADR to the spec's ADR list:

```bash
echo "docs/adr/[NNNN]-[title].md" >> specs/[spec-name]/.adr-list
```

### Step 7: Inform the User

Tell the user:
1. The ADR file path and number
2. Remind them the status is "Proposed" - they should:
   - Review and edit the ADR as needed
   - Commit it (ideally as the first commit on a feature branch)
   - Get team review
   - Use `/spec:approve design [NNNN]` when approved (changes status to "Accepted")


### Step 8: Review against the Design Principles

Read the ADR:

- Read ".agent_instructions/design_principles.md"
- Review the ADR using the design guidelines from "Use Responsibility-Driven Design"
- Check for a design focused on behavior: does the ADR think about *roles* and *responsibilities*.
- Responsibilities are "knowing", "doing", and "deciding"
- Allocate responbilities into roles, focusing on cohesion.
- Roles are interfaces or abstract types
- A class can implement one or more roles. If it implements multiple roles, they should be related.
- Provide feedback and suggest improvements

### Step 9. Suggest next steps:

Ask the user for the next step:

   - Create a feature branch if not already on one
   - Commit the ADR: `git commit -m "docs: add ADR for [title]"`
   - Continue with additional ADRs if needed
   - Or proceed to `/spec:tasks` if design is complete

## Important Guidelines

From [.agent_instructions/documentation.md](../../../.agent_instructions/documentation.md):

**ADR Best Practices**:
- One architectural decision per ADR - stay focused
- Focus on WHY, not just WHAT
- Document alternatives considered and why they were rejected
- Use diagrams (ASCII art or mermaid) where helpful
- Cross-reference related ADRs
- First ADR should be first commit on feature branch
- Create draft PR with ADRs for early feedback

**File Conventions**:
- Place in `docs/adr/` directory
- Use sequence number format: `NNNN-title.md` (4 digits with leading zeros)
- Use dash-case (kebab-case) for titles
- Scan existing ADRs to avoid duplication

## Notes

- ADRs should be created during the design phase, before implementation
- Multiple focused ADRs are better than one large ADR covering everything
- ADRs are immutable once accepted - supersede with new ADRs rather than editing
- The `/spec:design` command uses this same workflow when working with specifications
