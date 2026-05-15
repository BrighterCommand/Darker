# Architecture Decision Record (ADR) Commands

This directory contains Claude Code commands for creating and managing Architecture Decision Records in the Darker project.

## Commands

### `/adr <title>`

Creates a new Architecture Decision Record following Darker's template and conventions.

**Usage:**
```bash
/adr query caching strategy
```

**What it does:**

1. **Auto-numbers**: Scans `docs/adr/` to find the next sequence number
2. **Links to specs**: Automatically links to current specification if one exists
3. **Follows template**: Uses the standard ADR structure
4. **Gathers information**: Prompts for key ADR content (context, decision, alternatives, consequences)
5. **Creates file**: Generates properly named file in `docs/adr/[NNNN]-[title].md`
6. **Updates tracking**: Adds to spec's `.adr-list` if applicable

**File Naming Convention:**
- Format: `[NNNN]-[title].md`
- 4-digit sequence number with leading zeros (e.g., `0001`)
- Dash-case (kebab-case) title
- Example: `0002-query-caching-strategy.md`

## Integration with Specification Workflow

The `/adr` command integrates with the [specification workflow](../spec/README.md):

- **Standalone**: Use anytime you need to document an architectural decision
- **With specs**: Automatically links to current spec and updates `.adr-list`
- **Design phase**: Part of the `/spec:design` workflow

## Why Use ADRs?

Architecture Decision Records capture important design decisions that provide context to future reviewers and explorers of the codebase. They answer:

- **What** decision was made
- **Why** it was made (most important!)
- **What alternatives** were considered
- **What consequences** resulted
- **What context** influenced the decision

## Related Commands

- **`/spec:design [focus-area]`** - Same as `/adr` but within spec workflow context
- **`/spec:review design [NNNN]`** - Review a specific ADR
- **`/spec:approve design [NNNN]`** - Approve an ADR (changes status to "Accepted")
- **`/spec:status`** - Shows all specs and their ADR status

## References

- [Documentation Standards](../../../.agent_instructions/documentation.md)
- [Specification Workflow](../spec/README.md)
- [Michael Nygard's ADR article](http://thinkrelevance.com/blog/2011/11/15/documenting-architecture-decisions)
