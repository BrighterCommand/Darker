# 1. Record Architecture Decisions

Date: 2026-05-15

## Status

Accepted

## Context

**Scope**: Establish a practice for recording significant architectural decisions in the Darker project.

### The Problem

Darker is a mature library with several important architectural decisions embedded in the codebase. These decisions - why a decorator pipeline was chosen over middleware, why factories abstract DI containers, why sync and async paths are separate - are not recorded anywhere except implicitly in the code itself. New contributors and maintainers must reverse-engineer the rationale from the implementation.

Without a record of these decisions, we risk:
- Revisiting decisions that were already carefully considered
- Misunderstanding constraints that shaped the design
- Making changes that contradict the original intent without realising it

### Constraints

- ADRs should be lightweight enough that developers actually write them
- They should focus on WHY, not just WHAT
- They should be version-controlled alongside the code they describe

## Decision

We will use Architecture Decision Records (ADRs) as described by Michael Nygard in his article ["Documenting Architecture Decisions"](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions).

Each ADR will:
- Be stored in `docs/adr/` as a numbered markdown file
- Use the naming convention `NNNN-kebab-case-title.md`
- Follow a consistent template with Status, Context, Decision, Consequences, and Alternatives Considered sections
- Be immutable once accepted - superseded by new ADRs rather than edited

We are retroactively documenting existing architectural decisions to capture the rationale while it is still known.

## Consequences

### Positive

- Future contributors can understand WHY decisions were made, not just what the code does
- Architectural discussions are preserved in a searchable, reviewable format
- Decision rationale survives beyond the memory of original contributors

### Negative

- Adds a small overhead to the development process for new decisions
- Retroactive ADRs may not perfectly capture the original reasoning

## References

- Michael Nygard, ["Documenting Architecture Decisions"](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
- Brighter project's ADR practice (sibling project)
