---
allowed-tools: Bash(cat:*), Bash(test:*), Bash(touch:*), Bash(ls:*), Bash(echo:*), Bash(git:*), Bash(awk:*), Bash(date:*), Bash(basename:*), Bash(grep:*), Bash(mkdir:*), Read, Write, Edit, Glob, AskUserQuestion, Skill
description: Create technical design specification (ADR)
argument-hint: [adr-focus-area]
---

## Context

Architecture Decision Records: [Described by Michael Nygard](http://thinkrelevance.com/blog/2011/11/15/documenting-architecture-decisions)
ADR directory: `docs/adr/`

**Workflow**: Issue -> Requirements -> **ADR(s)** -> Tasks -> Tests -> Code

**Note**: You can create multiple ADRs for the same requirement. Each ADR should focus on a single architectural decision.

**Frontmatter**: Every ADR carries a YAML frontmatter block (`id`, `title`, `status`, `author`,
`created`, `summary`, `tags`) — see [`.agent_instructions/adr_frontmatter.md`](../../../.agent_instructions/adr_frontmatter.md).
This command reads existing ADRs' frontmatter to surface prior art (Step 3a) and stamps the new
ADR's frontmatter on write (Step 4a) via the `read_adr_metadata` / `write_adr_metadata` skills.

## Your Task

### Step 1: Verify Requirements Approved

1. Read `specs/.current-spec` to determine the active specification directory
2. Check for `.requirements-approved` file in the spec directory
3. If not approved:
   - Inform user to complete requirements first using `/spec:approve requirements`
   - Exit

### Step 2: Check Existing ADRs for this Spec

1. Check if `specs/{current-spec}/.adr-list` exists
2. If it exists, read it to see which ADRs are already associated with this spec
3. Show user the existing ADRs to avoid duplication

### Step 3: Determine ADR Number and Focus

1. Check existing ADRs: `ls docs/adr/ | grep -E "^[0-9]{4}-" | sort | tail -1`
2. If `docs/adr/` doesn't exist, create it: `mkdir -p docs/adr`
3. Calculate next number (format: 0001, 0002, 0003, etc.)
   - The number is an **ordering hint, not an identity** — the `id`/slug is the identity, and a
     number collision with a concurrent branch is acceptable (see `adr_frontmatter.md`).
4. Determine the ADR focus:
   - If $ARGUMENTS provided: Use that as the specific focus area
   - If not provided: Ask user what aspect of the requirement this ADR addresses
5. Create ADR filename: `docs/adr/{NNNN}-{focus-area}.md`
   - Use kebab-case for focus area
6. Add to tracking: `echo "{NNNN}-{focus-area}.md" >> specs/{current-spec}/.adr-list`

### Step 3a: Surface Prior-Art ADRs (before drafting)

Before drafting, find existing decisions relevant to **{focus-area}** so the new ADR reuses them,
avoids contradicting them, or deliberately supersedes them — rather than re-deciding in ignorance.

Use the `read_adr_metadata` skill (`.claude/commands/adr/read_adr_metadata.md`), passing the focus
area as the query plus any obvious tags from the taxonomy:

```
read_adr_metadata "{focus-area}" --tags "{likely-tags}"
```

It reads only frontmatter (cheap) and by default **skips `Deprecated` and `Superseded`** ADRs, so
retired decisions are not offered as live prior art. Reference the relevant candidates under
`Related ADRs`, and note any this ADR would supersede. Prefer this to reading `docs/adr/index.md`,
which is an un-ranked full-corpus table meant for human browsing.

If no prior art is found, note that and proceed.

### Step 4: Create ADR Document

Create `docs/adr/{NNNN}-{focus-area}.md` with the following template:

```markdown
# {Number}. {Title}

Date: {YYYY-MM-DD}

## Status

Proposed

## Context

{Describe the specific architectural problem this ADR addresses}

**Parent Requirement**: [specs/{spec-dir}/requirements.md](../../specs/{spec-dir}/requirements.md)

**Scope**: This ADR focuses specifically on {the architectural decision area}.

{Describe the problem and context}:
- What specific aspect of the requirement needs an architectural decision?
- What are the forces at play (technical, political, social, project)?
- Why is this decision important?
- What constraints exist?

## Decision

{Describe the specific architectural decision that was made}

- What approach are we taking for this aspect?
- What are the key technical choices?
- What patterns or practices will we follow?

### Architecture Overview

{Describe the architecture for this specific decision}
{Use ASCII art or mermaid diagrams where helpful}

### Key Components

{List and describe the main components affected by this decision}

### Technology Choices

{Document specific technology/library choices for this aspect and why}

### Implementation Approach

{Outline how this specific aspect will be implemented}

## Consequences

### Positive

- {What becomes easier or better with this decision?}

### Negative

- {What becomes harder or more complex?}

### Risks and Mitigations

- {Identify technical risks specific to this decision}
- {Describe how we'll mitigate them}

## Alternatives Considered

{Describe other approaches that were considered and why they were not chosen}

## References

- Requirements: [specs/{spec-dir}/requirements.md](../../specs/{spec-dir}/requirements.md)
- Related ADRs: {List related ADRs}
- External references: {Links to relevant documentation}
```

### Step 4a: Stamp Frontmatter and Regenerate the Index

After writing the ADR body:

1. **Stamp frontmatter** with the `write_adr_metadata` skill
   (`.claude/commands/adr/write_adr_metadata.md`), with a summary (1–2 sentences stating WHAT was
   decided) and 1–4 tags from the controlled taxonomy in [`.agent_instructions/adr_frontmatter.md`](../../../.agent_instructions/adr_frontmatter.md):

   ```
   write_adr_metadata docs/adr/{NNNN}-{focus-area}.md init --summary "{summary}" --tags "{tags}"
   ```

   This derives `id`/`title`/`created` from the file, sets `status: Proposed`, and keeps the
   frontmatter in sync with the body `## Status`. Confirm the checks the skill reports pass.

   If this ADR **supersedes** a prior ADR you identified in Step 3a, also mark that older ADR:

   ```
   write_adr_metadata docs/adr/{old-id}.md supersede --by {NNNN}-{focus-area}
   ```

2. **Regenerate the ADR index** so `docs/adr/index.md` reflects the new ADR (and any supersession):

   ```bash
   awk -f .claude/commands/adr/generate_adr_index.awk docs/adr/[0-9]*.md > docs/adr/index.md
   ```

   `docs/adr/index.md` is a regenerable cache — never hand-edit it; always regenerate from frontmatter.

3. **Recommend `/spec:write_release_notes` when this ADR breaks something.** If the ADR you just
   wrote or amended records, in its `## Consequences`, a change that breaks an existing behaviour
   or interface, tell the user so and recommend running `/spec:write_release_notes` for this spec.
   Do **not** run that command yourself — recommend it, and let the user choose when to run it.

### Step 5: Additional Guidance

When creating the ADR:
- Focus on **one specific architectural decision** - keep it focused
- Focus on the **why** of decisions, not just the **what**
- Use ASCII art or mermaid diagrams where helpful for architecture

### Step 6: Design Principles

Review against the design principles

- Read ".agent_instructions/design_principles.md"
- Review the ADR using the design guidelines from "Use Responsibility-Driven Design"
- Check for a design focused on behavior: does the ADR think about *roles* and *responsibilities*.
- Responsibilities are "knowing", "doing", and "deciding"
- Allocate responbilities into roles, focusing on cohesion.
- Roles are interfaces or abstract types
- A class can implement one or more roles. If it implements multiple roles, they should be related.
- Provide feedback and suggest improvements

### Step 7: Next Steps

1. Remind user to:
   - Review and complete the ADR with technical details
   - Commit the ADR and the regenerated index: `git add docs/adr/{NNNN}-{focus-area}.md docs/adr/index.md && git commit -m "docs: add ADR for {focus-area}"`
   - The first ADR should typically be the first commit on the feature branch
2. Multiple ADRs:
   - If requirement needs more architectural decisions, run `/spec:design [another-focus-area]` again
   - Each ADR should address a distinct architectural concern
3. When all ADRs are complete: `/spec:approve design` to proceed to tasks phase

Use the Write tool to create the ADR document in `docs/adr/`.
