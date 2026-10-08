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

## ADR Metadata (frontmatter & index)

Every ADR carries a YAML **frontmatter** block (`id`, `title`, `status`, `author`, `created`,
`summary`, `tags`) so agents can find prior art from cheap metadata instead of grepping full bodies.
The schema, status vocabulary, tag taxonomy, and identity rule are defined once in
[`.agent_instructions/adr_frontmatter.md`](../../../.agent_instructions/adr_frontmatter.md).

Two skills manage it (both are invoked *by* `/adr`, `/spec:design`, and `/spec:approve`, but can be
run directly):

- **`read_adr_metadata`** (`read_adr_metadata.md`) — reads only the frontmatter of each ADR and
  returns ranked prior-art candidates (`id` + `title` + `summary`) filtered by tag/status. Skips
  `Deprecated`/`Superseded` by default. Used before drafting to surface related decisions.
- **`write_adr_metadata`** (`write_adr_metadata.md`) — adds/updates a file's frontmatter and keeps it
  in sync with the body `## Status` (`init` → `Proposed`; `status Accepted` on approval;
  `supersede`/`deprecate` on retirement). Idempotent.

**Identity is the `id`/slug (filename stem), not the number.** ADR numbers are a non-unique ordering
hint — parallel branches can collide on numbers (two ADRs are numbered `0020`) and we do not
renumber. Tooling keys off `id`.

**Derived index** — `docs/adr/index.md` is a table generated *from* the frontmatter. It is a
regenerable cache: **never hand-edit it**. `/adr`, `/spec:design` and `/spec:approve` refresh it
automatically; to regenerate by hand run the single canonical command:

```bash
awk -f .claude/commands/adr/generate_adr_index.awk docs/adr/[0-9]*.md > docs/adr/index.md
```

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
