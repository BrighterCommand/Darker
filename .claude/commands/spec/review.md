---
allowed-tools: Bash(cat:*), Bash(test:*), Bash(touch:*), Bash(ls:*), Bash(echo:*), Bash(grep:*), Bash(mkdir:*), Bash(npx:*), Bash(python3:*), Bash(git diff:*), Bash(git log:*), Bash(git status:*), Bash(git rev-parse:*), Bash(git merge-base:*), Bash(git show:*), Read, Write, Glob, Grep, Agent, AskUserQuestion
description: Review current specification phase (requirements, design, tasks, or code)
argument-hint: [requirements|design [adr-number]|tasks|code [--base=ref]] [threshold]
---

## Adversarial Specification Review

Current spec directory: specs/

**Workflow**: Issue -> Requirements -> ADR(s) -> Tasks -> Tests -> Code -> **Code Review**

**Philosophy**: The first draft is never good enough. This review is skeptical and adversarial — it assumes problems exist and looks for them. The goal is to force iteration towards quality.

**Phases reviewable here**: `requirements`, `design`, `tasks`, and `code`. The first three review specification documents. `code` reviews the actual implementation against the approved specs — the diff vs. a base branch, cross-referenced against requirements.md + ADRs + tasks.md.

## Your Task

### Step 1: Parse Arguments and Determine What to Review

Read `specs/.current-spec` to determine the active specification directory.

**Error handling**: If `.current-spec` does not exist, tell the user to run `/spec:new` first and stop. If the spec directory doesn't exist, tell the user and stop. If the document for the requested phase doesn't exist, tell the user to run the appropriate creation command first and stop. Do NOT launch the sub-agent with missing documents.

Parse $ARGUMENTS:
- Extract **phase**: first word — `requirements`, `design`, `tasks`, or `code`
- Extract **adr-number**: for `design` phase, a zero-padded 4-digit number (e.g., `0053`). **Precedence rule**: a 4-digit zero-padded number is ALWAYS an ADR number, never a threshold.
- Extract **base-ref**: for `code` phase, a token starting with `--base=` (e.g., `--base=origin/master`). Default base is `master`.
- Extract **threshold**: any other numeric value (default: **60**)
- If phase is empty: auto-detect. Check approval markers in order: `.requirements-approved`, `.design-approved`, `.tasks-approved`. The first missing marker is the phase to review. If all three are approved, default to `code` (review the implementation against the approved specs). If no branch is checked out or there is no diff vs. base, tell the user there is nothing to review and suggest `/spec:status`.

**Approved phase warning**: If the user explicitly requests review of an already-approved phase, note this in the sub-agent prompt and include a note in the output: "This phase is already approved. Findings are informational — consider whether any warrant re-opening the phase." (Not applicable to `code` — code is never "approved" by the spec system.)

Examples:
- `/spec:review` -> auto-detect phase, threshold 60
- `/spec:review requirements` -> review requirements, threshold 60
- `/spec:review requirements 70` -> review requirements, threshold 70
- `/spec:review design 0053` -> review ADR 0053, threshold 60
- `/spec:review design 0053 80` -> review ADR 0053, threshold 80
- `/spec:review design 70` -> review all ADRs, threshold 70
- `/spec:review tasks 50` -> review tasks, threshold 50
- `/spec:review code` -> review branch diff vs. master, threshold 60
- `/spec:review code 70` -> review branch diff vs. master, threshold 70
- `/spec:review code --base=origin/master 70` -> diff vs. origin/master, threshold 70

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

**For code review:**
- Resolve `{base-ref}` (default `master`). Run `git rev-parse --verify {base-ref}` — if it fails, tell the user the base ref doesn't exist and stop.
- Run `git merge-base {base-ref} HEAD` to find the divergence point.
- Run `git diff --stat {base-ref}...HEAD` to get the summary of changed files.
- Run `git log --oneline {base-ref}..HEAD` to get the commit list.
- Run `git status --porcelain` to check for uncommitted changes (flag in findings if present).
- If the diff is empty, tell the user there is nothing to review vs. `{base-ref}` and stop.
- Read `specs/{current-spec}/requirements.md`, all ADRs from `.adr-list`, and `specs/{current-spec}/tasks.md`. These are the contracts the code must satisfy.
- Read `.agent_instructions/code_style.md`, `.agent_instructions/testing.md`, and `.agent_instructions/design_principles.md` — these encode project conventions the sub-agent must check against.
- The sub-agent itself will use `Bash(git diff:* / git show:*)`, `Read`, `Glob`, and `Grep` to pull specific file-level diffs and surrounding context. Do NOT attempt to inline the full diff in the sub-agent prompt — pass the file list, commit list, stats, and base ref, and let the sub-agent drill in where it needs to.

### Step 3: Launch Sub-Agent for Adversarial Review

**Verify scope with the user before launching (MAIN agent).** All user interaction stays in the main agent — never the sub-agent. If the review scope is ambiguous (which phase to review, which ADR for a design review, the base ref for a code review, the threshold to apply), confirm it with the user via `AskUserQuestion` before launching.

Launch an Agent (subagent_type: "general-purpose", **model: "opus"**) with the prompt below. Review is reasoning-heavy work, so it uses opus per the model policy (see `.claude/commands/spec/README.md` → "Sub-agents & model policy").

**Sub-agent tool access**: The sub-agent (general-purpose) inherits tool access. For design and tasks reviews it SHOULD use Read, Glob, and Grep to read documents and verify codebase references. For **design** reviews it SHOULD additionally use `Bash` to run the diagram render check and the escaped-markdown grep in the *Diagrams* criteria — those are verifications, not judgements, and a review that skips them has not checked the one class of defect that is invisible to a careful read. For **code** reviews it SHOULD additionally use `Bash(git diff:* / git show:* / git log:*)` to pull file-level diffs on demand, and MAY run `dotnet build Darker.Filter.slnf` / `dotnet test Darker.Filter.slnf` to check a claim about the build or the suite. The sub-agent should NOT write the findings file — it should return the findings as text. The main agent writes the file after validating the output.

**IMPORTANT**: The sub-agent prompt must include:
1. The review criteria for the relevant phase (from Step 4 below)
2. The full document text(s) or file paths to read
3. The cross-reference documents (when applicable)
4. The threshold value
5. The output format instructions (from Step 5 below)
6. An instruction to RETURN the findings as text output, NOT to write a file
7. An instruction to NOT ask the user any questions — review the supplied inputs and return
   findings; any clarification was handled by the main agent before launch

**Code review sub-agent prompt must additionally include:**
- The resolved base ref, the commit list, and the `git diff --stat` output
- The full text of requirements.md, tasks.md, and each ADR (so the sub-agent can cross-reference without re-reading)
- The paths to `.agent_instructions/code_style.md`, `testing.md`, `design_principles.md` (sub-agent reads them directly)
- An explicit instruction to drill into at least the top-N changed files (by size) using `git diff {base}...HEAD -- <file>` and to cite specific file:line references in every finding
- A reminder that uncommitted changes reported by `git status --porcelain` should be surfaced as at least a Medium finding

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

#### Code Review Criteria

You are a skeptical, **adversarial** reviewer. Your job is to find problems, not to validate work that has already been done. Assume:

- The author convinced themselves the code is right — they are not a reliable narrator.
- Tests that pass locally may still be wrong (asserting the wrong thing, testing the stub, or passing vacuously).
- Commit messages and PROMPT.md describe intent, not outcome — verify against the actual diff.

Every finding MUST cite concrete evidence: a file path, a line range, a diff hunk, or a spec section. No vibes.

**Requirement & ADR Fidelity (highest priority):**
- For each FR-N in requirements.md, locate where it is implemented in the diff. If you cannot find it, that is a finding (High or Critical depending on FR importance).
- For each AC, locate a test that asserts it. If the AC is satisfied by a unit test where the spec implies integration, downgrade trust and flag it.
- For each ADR decision, locate the corresponding code. Flag any deviation where the code does something different from the ADR without a documented reason.
- Flag scope creep: changed files or new abstractions that don't trace back to any FR, AC, or ADR decision.
- Flag scope holes: tasks in `tasks.md` marked done but with no visible code.

**TDD Compliance (Darker-specific):**
- For each behavioral change, confirm a test exists. Prefer finding the test commit BEFORE (or in the same commit as) the implementation in `git log {base}..HEAD` — that's the TDD signature. Absence isn't proof of violation, but combined with tests committed *after* implementation, it's a finding.
- Test naming: method and file `When_[condition]_should_[expected_behavior]`; class `[Behavior]Tests`. Violations are Low–Medium unless pervasive.
- Test doubles follow Real > Simple > InMemory > Mock: Moq used where a real registry, `SimpleHandlerFactory`, `SimpleHandlerDecoratorFactory`, `InMemoryDecoratorRegistry` or `InMemoryQueryContextFactory` would do is a finding. Mocks used to isolate a class (rather than replace I/O) are at least Medium.
- Test-specific handlers, queries and decorators belong in `test/Paramore.Darker.Tests/TestDoubles/` (namespace `Paramore.Darker.Tests.TestDoubles`), one class per file; shared doubles in `test/Paramore.Darker.Testing.Ports/`.
- Tests reach only public exports. `InternalsVisibleTo` added for testing is a High finding.
- Check for `[Fact(Skip=...)]`, commented-out `Assert`, or empty test bodies — any of these in the diff is at least Medium.

**Correctness:**
- Walk the logic of non-trivial new methods. Look for: off-by-one, null derefs, resource leaks (handlers, decorators, scopes or streams not released/disposed), async methods that swallow exceptions, race conditions in shared or static state (e.g. memoisation caches).
- For each new `if`/`else`, ask: is the else branch tested? Is there a condition where both branches are wrong?
- Exceptions raised through reflection must still surface unwrapped (the `ExceptionDispatchInfo` pattern in `QueryProcessor` / `PipelineBuilder`); a new path that leaks a `TargetInvocationException` is a finding.
- For new configuration/DI changes: is the lifetime correct (singleton vs scoped vs transient)? Are disposables registered correctly? Is anything released in `PipelineBuilder.Dispose()` that should be?

**Pipeline parity (Darker-specific):**
- Darker has parallel sync, async and streaming pipelines (handlers, decorators, attributes, factories, registries). A behavioural change on one path without its twin — or without a documented reason the twin does not need it — is a finding. Check that each twin also has its test.
- New decorators: step ordering via the attribute is respected, and the decorator is registered/resolved the same way on every path it claims to support.

**Security (apply even in "internal" code):**
- Injection or path traversal at any trust boundary (query parameters, context bag values, file names, connection strings)
- Secrets or connection strings committed to code, logs, or test fixtures
- Unsafe deserialization of attacker-controlled data; query-logging that writes sensitive query contents
- Weak crypto or `Random` used for security-sensitive values

**Project Conventions (read `.agent_instructions/code_style.md` yourself to verify):**
- Primary constructors where possible for new classes (project default)
- `Async` suffix on async methods; `ValueTask` vs `Task` consistency with surrounding code
- Nullability annotations consistent with the file's existing style
- XML doc comments on public APIs, and the MIT licence header on new files (project requires them)
- No dead code, speculative abstractions, or commented-out blocks
- No mixing of structural and behavioral changes in the same commit (tidy-first rule)
- Package versions managed centrally in `Directory.Packages.props` — a `Version=` attribute on a project `PackageReference` is a finding

**Hygiene:**
- `git status --porcelain` non-empty → Medium finding ("uncommitted work on branch at time of review").
- New files that look misplaced (e.g., test files in src/, or vice versa).
- Binary files, generated files, or `.DS_Store` committed by accident.
- Build warnings introduced (if the diff touches a project the user has recently built, note whether warnings regressed).

**Grounding — no hallucinated findings:**
- Before filing a finding, verify: did you actually read the file, or are you guessing from the name? If guessing, either read it or don't file the finding.
- If the diff references a class or method, grep for it in the actual codebase (post-change) to confirm it exists as described.
- A finding that says "you should have done X" when X is already present is worse than no finding — it destroys trust in the whole review. Prefer fewer, grounded findings over comprehensive-looking speculation.

---

### Step 5: Output Format for Sub-Agent

Tell the sub-agent to produce output in this exact format:

```markdown
# Review: {phase} — {spec-name}

**Date**: {today's date}
**Threshold**: {threshold}
**Verdict**: {PASS or NEEDS WORK}
{For code reviews: **Base**: {base-ref} | **Head**: {HEAD commit sha}}

{If NEEDS WORK: "N findings at or above threshold {threshold}. Address these before approving."}
{If PASS: "No findings at or above threshold {threshold}. Consider addressing lower-scored items."}

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

**Calibration for code reviews:**
- A FR or ADR decision with no corresponding code IS High or Critical (70-95), depending on the FR's importance.
- A behavioural change on one of the sync, async or streaming pipeline paths without its twin IS High (70-80), unless the spec documents why the twin does not need it.
- A test that passes vacuously, or asserts the stub rather than the behaviour, IS High (70-85) — it reads as coverage and is not.
- A mock used to isolate a class where a real/Simple/InMemory double exists IS Medium (55-65).
- Naming-convention drift in tests is Low-Medium (35-55) unless pervasive.

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

1. **Validate the output** before writing: the expected sections are present, the Summary counts
   match the findings listed (count them yourself), and the verdict is consistent with the threshold
2. **Write the findings file** to `specs/{current-spec}/review-{phase}.md`. For the `code` phase the
   file is `specs/{current-spec}/review-code.md`.
3. **Present a summary to the user**: verdict, counts by severity, the title and score of each
   finding at or above threshold, and the path to the findings file.
   - For `requirements`/`design`/`tasks`: remind them to use `/spec:approve {phase}` when ready, or
     to work the findings (Step 8) and re-run `/spec:review {phase}`.
   - For `code`: remind them that `code` has no approval marker — fix findings, commit, and re-run
     `/spec:review code` until clean. When clean, the next step is commit/push/PR.

### Step 7: Spec Status

Display overall spec status:
- Spec directory: `specs/{current-spec}`
- Requirements: Approved / In Progress
- Design: {X} ADRs ({Y} approved, {Z} proposed)
- Tasks: Approved / In Progress / Not Started
- Code: Reviewed at {commit sha} / Not reviewed (code is "reviewed" if `review-code.md` exists; include its verdict and the commit sha at the top of HEAD when the review was run)

### Step 8: Working the Findings — Fix Issues, Not Lines

This step is not part of the review. It applies whenever the findings of a `requirements`, `design`
or `tasks` review are worked, in this session or a later one, and it runs only when the user asks
for them to be addressed.

**Why it exists.** A finding quotes one sentence, but the concept that sentence states is usually
stated in several places: a definition, an FR, an NFR, a constraint, Out of Scope, one or more ACs,
a worked example, and, for a design, sibling ADRs. Editing only the quoted sentence leaves the other
places saying the old thing, so the fix becomes a new contradiction. The next pass then finds it,
often scored *higher* than the original finding, because a contradiction outranks a vagueness.
Fixing lines instead of issues is how a review loop stops converging.

1. **Name the issue, not the sentence.** For each finding being worked, write down in a few words
   the concept it is about: "who may add a release-notes marker", not "line 121". Findings that
   turn out to be about the same concept are worked together.
2. **Find every statement of the concept.** Grep the whole document with several search terms, not
   only the words the finding quoted. For a design, grep every ADR in `.adr-list` and
   `requirements.md` too. Check Definitions, FRs, NFRs, Constraints, Out of Scope, the ACs and any
   worked examples. List the sites before editing any of them.
3. **Edit every site together**, so the concept is stated one way everywhere. When a fix needs a
   choice, prefer the option that does not contradict a rule stated elsewhere. If every option
   does, or the choice is the owner's, ask the user before editing.
4. **Re-sweep the old phrasings.** Grep for the wording you replaced and read every hit. Fix any
   survivor that is now false.
5. **Run a quick contradiction check before committing.** Launch a sub-agent
   (`general-purpose`, `model: "opus"`, forbidden to ask the user anything or to write files). Give
   it the diff of the edit, the findings file and the list of concepts touched. Its job is narrow
   and is not a re-review of the document. For each concept it:
   - greps every statement of the concept;
   - reports any sentence, edited or untouched, that now disagrees with another;
   - confirms which findings are resolved;
   - flags new wording that two implementers would read differently.

   Fix anything it scores **≥ 50** before committing, repeating steps 2–4 for that concept. You may
   fix lower items in the same edit when they sit in a concept already touched.
6. **Commit, then re-run `/spec:review {phase}`** as the next pass. The commit message names the
   issues worked, not only the finding numbers.
