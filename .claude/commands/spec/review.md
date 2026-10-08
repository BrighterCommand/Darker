---
allowed-tools: Bash(cat:*), Bash(test:*), Bash(touch:*), Bash(ls:*), Bash(echo:*), Bash(grep:*), Bash(mkdir:*), Bash(npx:*), Bash(python3:*), Read, Write, Glob, Grep, Agent
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
- Pass the path to `.agent_instructions/documentation.md` — § *Architecture Decision Records* is
  the canonical skeleton, readability rules, diagram rules and pre-commit checks that the
  *Structure and Readability* criteria in Step 4 grade against, and § *Writing tone for design
  documents* is the tone contract. The sub-agent reads it itself.
- **Even when reviewing a single ADR, read the whole `.adr-list` set.** Two of the design criteria
  — the sibling map and the unifying sentence — are properties of the set, not of one file, and
  cannot be judged from one ADR alone.

**For tasks review:**
- Read `specs/{current-spec}/tasks.md`
- Read `specs/{current-spec}/requirements.md` (for cross-referencing)
- Read `specs/{current-spec}/.adr-list` and each ADR (for cross-referencing)

### Step 3: Launch Sub-Agent for Adversarial Review

Launch an Agent (subagent_type: "general-purpose") with the prompt below.

**Sub-agent tool access**: The sub-agent (general-purpose) inherits tool access. For design and tasks reviews it SHOULD use Read, Glob, and Grep to read documents and verify codebase references. For **design** reviews it SHOULD additionally use `Bash` to run the diagram render check and the escaped-markdown grep in the *Diagrams* criteria — those are verifications, not judgements, and a review that skips them has not checked the one class of defect that is invisible to a careful read. The sub-agent should NOT write the findings file — it should return the findings as text. The main agent writes the file after validating the output.

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

**Structure and Readability:**

An ADR that is correct and unreadable has failed. Its primary audience is a human reviewer six
months from now reading for the *why* of a behaviour — behaviour endures, structure changes under
refactoring. Grade against `.agent_instructions/documentation.md` § *ADR structure* and
§ *ADR readability*:

- **Headings.** Do they match the canonical skeleton — same wording, same order, same nesting
  level as the sibling ADRs? Heading drift is what makes a corpus unnavigable, and an ADR that
  invents its own section names or nests them one level off its siblings is a finding even when
  every word of its content is right.
- **Behaviour before structure.** Does the Decision open with `### The mechanism, end to end`
  (what happens, in what order) before `### Where the pieces live` (which assemblies) before the
  signatures? An ADR whose Decision opens with an interface declaration has it backwards.
- **The opening.** Does Context open in plain language a user would recognise, or does it name
  four interfaces before the reader knows what the problem is?
- **Orientation first.** Does each section lead with the artefact that orients — a table, a
  diagram, a contract — and then read the consequences off it? Or does it make the reader assemble
  the picture from paragraphs and only then show them the diagram?
- **The roles table.** Is there a `#### The roles, and what each is responsible for` table with
  **knowing** / **doing** / **deciding** stereotypes? If it is absent, or its rows are types rather
  than roles, Responsibility-Driven Design was applied to the review and not to the design.
- **Citation density.** Are `file:line` references concentrated in `Implementation Approach` and
  the `Where each type is touched` table? Citations are load-bearing for an implementor and pure
  noise inside an argument — a forces bullet carrying three of them is a finding.
- **What is unchanged.** Does `Where each type is touched` close by naming the types deliberately
  *not* touched, so a reviewer does not read an omission as an oversight?
- **One decision.** Can the ADR state its unifying rule in one sentence? If that takes a paragraph,
  suspect it is more than one decision and say so.

**Diagrams — verify these actively, do not eyeball them:**

- **Render every mermaid block.** Extract each to its own `.mmd` file under the scratchpad and run
  `npx -y -p @mermaid-js/mermaid-cli@11 mmdc -i diagram.mmd -o diagram.svg`. A non-zero exit or a
  missing `.svg` is a **defect in the ADR** — report it at High or above. Roughly one diagram in
  six fails on first draft, and a broken diagram looks perfectly fine in a read-through. If the
  render cannot run at all (no network for `npx`), say so in the findings rather than reporting the
  diagrams as verified.
- The three traps that produce a clean-looking, non-rendering diagram: **`;` is a statement
  separator in `sequenceDiagram`**; **`<` or `>` in a label** is eaten as HTML, so `Lease<T>` loses
  its type parameter; **HTML entities** (`&lt;`, `&gt;`, `&amp;`) break the escaped-markdown check.
- **Escaped markdown**: `grep -c '&lt;\|&gt;\|&amp;' docs/adr/{file}.md` must be `0`.
- **Is any flowchart carrying more than about four decision points?** That renders as an unreadable
  column with edges spanning its whole height. It should be a decision-ladder table — one row per
  situation, in evaluation order. A diagram can parse cleanly and still be unreadable, so render
  the most complex one to PNG (`-o diagram.png -w 1600 -b white`) and look at it.

**Writing tone** (§ *Writing tone for design documents*):

- Does the ADR reference **participants in the authoring conversation** — "at the user's
  direction", "the user explicitly chose", "per the user's feedback" — rather than durable
  artefacts like requirement ids, other ADRs or code locations? This defect has survived into
  Accepted ADRs before, so check for it rather than assuming it away.
- Does it reference **ephemeral state** — `PROMPT.md`, the current spec phase, unresolved review
  back-and-forth? Either the substance is folded in or the sentence goes.

**Frontmatter:**
- Does each ADR carry the YAML frontmatter defined in `.agent_instructions/adr_frontmatter.md`
  (`id` == filename stem, `status` matching the body `## Status`, a `summary` that states what was
  decided, 1–4 tags from the controlled taxonomy)?

**Release Notes Coverage:**
- Does the ADR set (or any ADR in it) record, in its `## Consequences`, a change that breaks an
  existing behaviour or interface? If so, does `release_notes.md` carry a section marked for this
  spec (a `### … (spec {id})` heading immediately followed by
  `<!-- spec: {this spec's directory name} -->`)? A breaking change with no such marked section is
  a finding — recommend running `/spec:write_release_notes`.

**Cross-Reference (when reviewing all ADRs, or one ADR within a set):**
- Do the ADRs collectively cover all the requirements?
- Are there gaps — requirements that no ADR addresses?
- Are there contradictions between ADRs?
- **Does every ADR in the set carry `### Where this ADR sits`, and is every table complete?** ADRs
  are written one at a time, so an earlier ADR's map goes stale the moment the next one lands —
  check each table lists *every* ADR in the set, with that file's own row bolded and marked
  *(this one)*. A map that is right for the newest ADR and stale for the rest is worse than no map.
- **Is the unifying sentence stated identically across the siblings that apply it**, or has it been
  paraphrased into three not-quite-equivalent versions?

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

**Task-Type Tag Form:**
- Does every task checkbox open its bold lead-in with the tag first (`**STRUCTURAL: T1.2 —
  …**`), never the task id first (`**T1.2 — STRUCTURAL: …**`)? `/spec:ralph-implement` dispatches
  on the leading label and skips a task it cannot classify — flag any checkbox that drifts, outside
  a *DO NOT* block.
- Does every behavioral task carry the `⛔ APPROVAL GATE` line, and does no task try to encode the
  review gear (a "no gate" annotation, an omitted gate line)? The gear lives in `.current-gear`.
- Does every `CHARACTERISE` task name its RED mutation (the production change and the assertion it
  must fail on)?

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

**Calibration for design reviews:**
- **A mermaid diagram that does not render IS High (70-85)** — it is a broken artefact that reads as perfectly fine, so nothing else will catch it before it ships.
- **An ADR that references a participant in the authoring conversation** ("at the user's direction", "the user explicitly chose") **IS Medium-High (65-75)** — it fails the document's only audience, and once the ADR is Accepted the sentence tends to stay.
- **A stale `### Where this ADR sits` map** — one that omits a sibling added after it was written — **IS Medium-High (60-75)**. A reader who finds a map stale once stops trusting every other map in the corpus.

Calibrating the structure criteria specifically — they are about navigability, not conformity:
- Heading **order or nesting level** drifting from the siblings is Medium-High (60-75): it breaks the reader's ability to navigate the next ADR using what they learned on this one.
- Heading **wording** differing slightly while order and nesting are right is Low-Medium (35-55).
- **Do not demand an artefact where there is nothing to put in it.** An ADR that genuinely introduces no new roles — a pure ordering, timing or naming decision — needs no roles table, and a missing one there is Low or no finding at all. The same goes for a diagram on an ADR whose decision has no sequence and no layering. Report the *absence of orientation*, not the absence of a specific heading.

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
