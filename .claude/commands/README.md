# Claude Code Skills for Darker Development

This directory contains Claude Code skills (slash commands) that enforce Darker's engineering practices and streamline common development workflows.

## Quick Start

Skills are invoked using slash commands in Claude Code:

```bash
/test-first <behavior description>    # TDD with mandatory approval
/adr <title>                          # Create Architecture Decision Record
/tidy-first <change description>      # Separate structural from behavioral changes
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

## Skill Categories

### Development Workflow Skills
- **`/test-first`** - TDD with approval gate
- **`/tidy-first`** - Safe refactoring workflow

### Documentation Skills
- **`/adr`** - Architecture Decision Records

### Specification Workflow Skills
- **`/spec:requirements`** - Capture requirements
- **`/spec:design`** - Create design ADRs
- **`/spec:tasks`** - Break down implementation
- **`/spec:implement`** - TDD implementation
- **`/spec:status`** - Show spec status
- **`/spec:approve`** - Approve phases
- **`/spec:review`** - Review phases

Documentation: [.claude/commands/spec/README.md](spec/README.md)

---

## When to Use Which Skill

### Decision Tree

```
Do you need to document an architectural decision?
+- Yes -> /adr <title>
+- No
   Are you adding new behavior or fixing a bug?
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

Three core skills enforce Darker's mandatory engineering practices:

| Skill | Enforces | Creates |
|-------|----------|---------|
| `/test-first` | TDD with approval | Tests -> Implementation -> Refactoring |
| `/adr` | Documented decisions | Numbered ADR files |
| `/tidy-first` | Structural/behavioral separation | Two commits: refactor + feat |

**Key insight**: These skills make the **correct approach the easy path** by automating multi-step workflows and enforcing approval gates.
