---
allowed-tools: Bash(cat:*), Bash(test:*), Bash(touch:*), Bash(ls:*), Bash(echo:*), Bash(grep:*), Read, Write, Glob, Grep, Agent
description: Review current specification phase
argument-hint: [requirements|design [adr-number]|tasks] [threshold]
---

## Adversarial Specification Review

Current spec directory: specs/

**Workflow**: Issue -> Requirements -> ADR(s) -> Tasks -> Tests -> Code

**Philosophy**: The first draft is never good enough. This review is skeptical and adversarial — it assumes problems exist and looks for them. The goal is to force iteration towards quality.

## Your Task

### Step 1: Parse Arguments and Determine What to Review

Read `specs/.current-spec` to determine the active specification directory.

**Error handling**: If `.current-spec` does not exist, tell the user to run `/spec:new` first and stop. If the spec directory doesn't exist, tell the user and stop. If the document for the requested phase doesn't exist, tell the user to run the appropriate creation command first and stop. Do NOT launch the sub-agent with missing documents.

Parse $ARGUMENTS:
- Extract **phase**: first word — `requirements`, `design`, or `tasks`
- Extract **adr-number**: for `design` phase, a zero-padded 4-digit number (e.g., `0053`). **Precedence rule**: a 4-digit zero-padded number is ALWAYS an ADR number, never a threshold.
- Extract **threshold**: any other numeric value (default: **60**)
- If phase is empty: auto-detect (first unapproved phase by checking `.requirements-approved`, `.design-approved`, `.tasks-approved` markers). If ALL phases are approved, tell the user all phases are approved and suggest `/spec:status` or `/spec:implement` instead.

**Approved phase warning**: If the user explicitly requests review of an already-approved phase, note this in the sub-agent prompt and include a note in the output.

Examples:
- `/spec:review` -> auto-detect phase, threshold 60
- `/spec:review requirements` -> review requirements, threshold 60
- `/spec:review requirements 70` -> review requirements, threshold 70
- `/spec:review design 0053` -> review ADR 0053, threshold 60
- `/spec:review design 0053 80` -> review ADR 0053, threshold 80
- `/spec:review design 70` -> review all ADRs, threshold 70
- `/spec:review tasks 50` -> review tasks, threshold 50

### Step 2: Gather Documents for the Sub-Agent

Read ALL documents the sub-agent will need. The sub-agent gets a clean context — it only knows what you send it.

**For requirements review:**
- Read `specs/{current-spec}/requirements.md`
- Read `.issue-number` if it exists (for context)

**For design review:**
- Read `specs/{current-spec}/.adr-list` to find ADRs
- Read each ADR from `docs/adr/{filename}` (or just the specified one)
- Read `specs/{current-spec}/requirements.md` (for cross-referencing)

**For tasks review:**
- Read `specs/{current-spec}/tasks.md`
- Read `specs/{current-spec}/requirements.md` (for cross-referencing)
- Read `specs/{current-spec}/.adr-list` and each ADR (for cross-referencing)

### Step 3: Launch Sub-Agent for Adversarial Review

Launch an Agent (subagent_type: "general-purpose") with the prompt below.

**Sub-agent tool access**: The sub-agent (general-purpose) inherits tool access. For design and tasks reviews it SHOULD use Read, Glob, and Grep to read documents and verify codebase references. The sub-agent should NOT write the findings file — it should return the findings as text. The main agent writes the file after validating the output.

**IMPORTANT**: The sub-agent prompt must include:
1. The review criteria for the relevant phase (from Step 4 below)
2. The full document text(s) or file paths to read
3. The cross-reference documents (when applicable)
4. The threshold value
5. The output format instructions (from Step 5 below)
6. An instruction to RETURN the findings as text output, NOT to write a file

### Step 4: Phase-Specific Review Criteria

Include the relevant criteria block in the sub-agent prompt.

---

#### Requirements Review Criteria

You are a skeptical reviewer. Assume the requirements have problems — your job is to find them.

**Testability and Concreteness:**
- Does every functional requirement have at least one concrete example?
- Can each acceptance criterion be turned directly into a test assertion?
- Are boundary conditions explicitly stated?
- Are error scenarios specified?

**Completeness:**
- Is there a clear problem statement with a user story?
- Are functional requirements listed and numbered?
- Are non-functional requirements specified?
- Are constraints and assumptions documented?
- Is the out-of-scope section explicit?

**Unambiguity:**
- Is terminology consistent throughout? Are key terms defined?
- Are there vague phrases like "should handle gracefully", "supports multiple", "works as expected"?
- Could two developers read a requirement and implement it differently?

**Boundedness:**
- Is the scope clearly bounded?
- Are there contradictions between sections?

**Acceptance Criteria Quality:**
- Does every FR map to at least one AC?
- Are ACs written in testable format?

---

#### Design (ADR) Review Criteria

You are a skeptical reviewer. Assume the design has problems — your job is to find them.

**Grounding in Reality:**
- Do file path references actually exist in the codebase? USE Glob and Grep to verify.
- Are class/type names accurate? Search the codebase to confirm.
- Does the design reference real existing patterns in the codebase?

**Decision Quality:**
- Is the Context section specific about the architectural problem?
- Does the Decision section explain WHY, not just WHAT?
- Are consequences (both positive AND negative) documented honestly?

**Connection to Requirements:**
- Does the ADR reference specific requirements it addresses?
- Does the design introduce scope beyond what the requirements ask for?

**Completeness:**
- Is the error handling strategy explicit?
- Are concurrency/threading concerns addressed where relevant?

---

#### Tasks Review Criteria

You are a skeptical reviewer. Assume the task list has problems — your job is to find them.

**Independent Verifiability:**
- Does each task produce a verifiable result?
- Can each task be verified WITHOUT running the whole system?

**Test-First Framing:**
- Does every behavioral task follow the pattern: write test -> get approval -> implement -> refactor?
- Does each TEST task specify a `/test-first` command?

**Ordering and Dependencies:**
- Are dependencies between tasks explicit?
- Are structural/tidy tasks before behavioral tasks?

**Granularity:**
- Is each task small enough to complete in one agent session?
- Could any task be broken into smaller pieces?

**Coverage Cross-Reference:**
- Map each FR from requirements.md to tasks. Are there FRs with no corresponding task? LIST THEM.
- Map each ADR decision to tasks. Are there design decisions with no implementation task? LIST THEM.

---

### Step 5: Output Format for Sub-Agent

Tell the sub-agent to produce output in this exact format:

```markdown
# Review: {phase} — {spec-name}

**Date**: {today's date}
**Threshold**: {threshold}
**Verdict**: {PASS or NEEDS WORK}

## Findings

### {N}. {Short title} (Score: {0-100})

{Description of the problem.}

**Evidence**: {Quote the problematic text or reference the specific gap.}

**Recommendation**: {How to fix it.}

---

## Summary

| Score Range | Count |
|-------------|-------|
| 90-100 (Critical) | {n} |
| 70-89 (High) | {n} |
| 50-69 (Medium) | {n} |
| 0-49 (Low) | {n} |

**Total findings**: {n}
**Findings at or above threshold ({threshold})**: {n}
```

**Scoring guide:**
- **90-100 (Critical)**: Blocks approval. Real defect, contradiction, or broken reference.
- **70-89 (High)**: Should fix before approval. Significant gap or ambiguity.
- **50-69 (Medium)**: Worth addressing. Vagueness, missing examples.
- **0-49 (Low)**: Suggestion or nit.

**Verdict logic**: If ANY finding scores >= threshold -> NEEDS WORK. Otherwise -> PASS.

### Step 6: Validate, Write Findings, and Present Summary

After the sub-agent returns:

1. **Validate the output** before writing
2. **Write the findings file** to `specs/{current-spec}/review-{phase}.md`
3. **Present a summary to the user**

### Step 7: Spec Status

Display overall spec status:
- Spec directory: `specs/{current-spec}`
- Requirements: Approved / In Progress
- Design: {X} ADRs ({Y} approved, {Z} proposed)
- Tasks: Approved / In Progress / Not Started
