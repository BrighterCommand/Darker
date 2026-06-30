# Claude Code Skills for Darker Development

This directory contains Claude Code skills (slash commands) that enforce Darker's engineering practices and streamline common development workflows.

## Quick Start

Skills are invoked using slash commands in Claude Code:

```bash
/test-first <behavior description>    # TDD with mandatory approval
/adr <title>                          # Create Architecture Decision Record
/tidy-first <change description>      # Separate structural from behavioral changes
/bugfix:triage <issue | description>  # Diagnosis-first bug workflow (Confirm gate)
```

## Available Skills

### 1. Test-Driven Development

**Command**: `/test-first <behavior description>`

**Purpose**: Enforces TDD workflow with mandatory user approval before implementation.

**When to use**:
- Adding new behavior or functionality
- Fixing bugs with test-first approach
- Want to ensure tests are correct before writing implementation

**Workflow**:
1. RED: Claude writes a failing test following Darker conventions
2. APPROVAL: You must approve the test before implementation
3. GREEN: Claude implements minimum code to pass the test
4. REFACTOR: Claude suggests design improvements (optional)

**Example**:
```bash
/test-first when query handler throws it should invoke fallback policy
```

**Why it matters**: The approval step is MANDATORY per testing.md when working with AI. This skill enforces that requirement, preventing implementation before you validate the test specification.

Documentation: [.claude/commands/tdd/README.md](tdd/README.md)

---

### 2. Architecture Decision Records

**Command**: `/adr <title>`

**Purpose**: Automates creation of properly formatted and numbered ADRs.

**When to use**:
- Making significant architectural decisions
- Need to document WHY a design choice was made
- Want to capture alternatives considered

**What it does**:
1. Scans `docs/adr/` to find next sequence number
2. Checks for current spec and links if applicable
3. Prompts for key ADR content (context, decision, alternatives, consequences)
4. Creates properly named file: `docs/adr/[NNNN]-[title].md`
5. Updates spec's `.adr-list` if part of spec workflow

**Example**:
```bash
/adr query caching strategy
```

Documentation: [.claude/commands/adr/README.md](adr/README.md)

---

### 3. Tidy First - Separate Structural from Behavioral Changes

**Command**: `/tidy-first <change description>`

**Purpose**: Enforces Beck's "Tidy First" methodology by separating refactoring from functionality changes into distinct commits.

**When to use**:
- Need to refactor code AND add/change functionality
- Existing code is messy and needs cleanup before modification
- Want cleaner git history and easier code reviews

**Workflow**:
1. **Analysis**: Categorizes changes into structural (refactoring) vs behavioral (functionality)
2. **Plan**: Gets your approval of categorization
3. **Structural Phase**: Makes refactoring changes only
4. **Validate**: Runs tests - all must pass (behavior unchanged)
5. **Commit**: Creates `refactor:` commit
6. **Behavioral Phase**: Makes functionality changes
7. **Validate**: Runs tests with new behavior
8. **Commit**: Creates `feat:`/`fix:`/`perf:` commit

**Example**:
```bash
/tidy-first optimize the pipeline building in QueryProcessor
```

Documentation: [.claude/commands/refactor/README.md](refactor/README.md)

---

### 4. Bugfix - Diagnosis-First Bug Workflow

**Commands**: `/bugfix:triage`, `/bugfix:confirm`, `/bugfix:test`, `/bugfix:fix`, `/bugfix:verify` (plus `/bugfix:status`, `/bugfix:switch`)

**Purpose**: A lightweight, diagnosis-first workflow for fixing bugs. It is `/test-first` wrapped with an explicit **Confirm** gate up front — because a bug's root cause is a hypothesis until proven.

**When to use**:
- A defect whose root cause is not yet proven
- An issue that arrived with a suggested fix (including agent-authored) you should verify before trusting
- Anywhere `/test-first` alone would jump to a test for an *assumed* cause

**Workflow**:
1. **Triage** (`/bugfix:triage [issue|description]`) - Restate the symptom, locate the code, form a root-cause hypothesis (any suggested fix is UNVERIFIED)
2. ✋ **Confirm** (`/bugfix:confirm`) - Prove the hypothesis by code-trace and/or red repro before any fix; surfaces scope changes / extra defects (incl. sync/async pipeline parity)
3. ✋ **Test-first** (`/bugfix:test`) - Delegates to `/test-first` for the failing regression test
4. **Fix** (`/bugfix:fix`) - Minimal change to green, scoped to the confirmed cause
5. **Verify** (`/bugfix:verify`) - Run the suite; capture the root cause and `Fixes #N` in the commit/PR

**Example**:
```bash
/bugfix:triage 123      # [FallbackPolicy] not catching async-handler exceptions
/bugfix:confirm         # proves TargetInvocationException unwrap-order cause; finds a sync-path twin
/bugfix:test            # red regression test (via /test-first)
/bugfix:fix             # minimal fix scoped to the confirmed cause
/bugfix:verify          # suite green; fix: commit with Fixes #123
```

**Why it matters**: The Confirm gate stops you fixing a symptom or trusting a wrong suggested fix — and frequently changes the scope of the fix. It deliberately omits the ADR/requirements/review rounds that `/spec` mandates.

Documentation: [.claude/commands/bugfix/README.md](bugfix/README.md)

---

## Skill Categories

### Development Workflow Skills
- **`/test-first`** - TDD with approval gate
- **`/tidy-first`** - Safe refactoring workflow
- **`/bugfix:*`** - Diagnosis-first bug workflow (Triage → Confirm → Test-first → Fix → Verify)

### Documentation Skills
- **`/adr`** - Architecture Decision Records

### Specification Workflow Skills
- **`/spec:requirements`** - Capture requirements
- **`/spec:design`** - Create design ADRs
- **`/spec:tasks`** - Break down implementation (attended path)
- **`/spec:implement`** - TDD implementation (attended path)
- **`/spec:ralph-tasks`** - Generate unattended TDD tasks (standalone, from approved design)
- **`/spec:ralph-implement`** - Unattended self-driving TDD loop (opus + auto mode)
- **`/spec:status`** - Show spec status
- **`/spec:approve`** - Approve phases (prompts the attended/unattended fork at design)
- **`/spec:review`** - Review phases

Documentation: [.claude/commands/spec/README.md](spec/README.md)

---

## When to Use Which Skill

### Decision Tree

```
Do you need to document an architectural decision?
+- Yes -> /adr <title>
+- No
   Are you fixing a bug?
   +- Yes
   |   Is the root cause already proven/obvious?
   |   +- No  -> /bugfix:triage  (Triage -> Confirm gate -> Test-first -> Fix -> Verify)
   |   +- Yes -> /test-first <behavior>  (cause is clear; just need the test)
   +- No
       Are you adding new behavior?
       +- Yes
       |   Does existing code need refactoring first?
       |   +- Yes -> /tidy-first <description>
       |   +- No -> /test-first <behavior>
       +- No
           Are you just refactoring with no behavior changes?
           +- Yes -> /tidy-first <description> (will create single refactor commit)
           +- No -> Use standard workflow
```

---

## Integration with Darker Practices

These skills enforce practices documented in `.agent_instructions/`:

| Skill | Enforces | Reference |
|-------|----------|-----------|
| `/test-first` | TDD approval workflow | [testing.md](../../.agent_instructions/testing.md) |
| `/adr` | ADR creation standards | [documentation.md](../../.agent_instructions/documentation.md) |
| `/tidy-first` | Structural/behavioral separation | [code_style.md](../../.agent_instructions/code_style.md) |

All three make **mandatory workflows enforceable** rather than just documented.

---

## Getting Help

- **Skill documentation**: Each skill has a README.md in its directory
- **Darker guidelines**: See `.agent_instructions/` for full practices
- **Issues**: Report skill issues at https://github.com/anthropics/claude-code/issues

---

## Summary

These core skills enforce Darker's mandatory engineering practices:

| Skill | Enforces | Creates |
|-------|----------|---------|
| `/test-first` | TDD with approval | Tests -> Implementation -> Refactoring |
| `/adr` | Documented decisions | Numbered ADR files |
| `/tidy-first` | Structural/behavioral separation | Two commits: refactor + feat |
| `/bugfix:*` | Confirm root cause before fixing | Bug record + regression test + scoped `fix:` commit |

**Key insight**: These skills make the **correct approach the easy path** by automating multi-step workflows and enforcing approval gates.
