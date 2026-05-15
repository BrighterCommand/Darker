# Tasks: Remove Third-Party DI Packages

**Spec**: 001-remove_di_packages
**ADR**: [0006-remove-third-party-di-packages](../../docs/adr/0006-remove-third-party-di-packages.md)
**Issue**: #295

## Task Order

This is a removal task, not a feature addition. No new behavior is being introduced, so there are no TEST + IMPLEMENT tasks. The work is structural: delete projects, remove references, update documentation, and verify the build still passes.

Tasks are ordered to maintain a buildable solution at each step.

---

## Tasks

### Phase 1: Remove Source Projects and References (Structural)

- [ ] **1. Remove SimpleInjector and LightInject project references from test projects**
  - Remove `ProjectReference` to `Paramore.Darker.SimpleInjector` from `test/Paramore.Darker.Tests/Paramore.Darker.Tests.csproj`
  - Remove `ProjectReference` to `Paramore.Darker.LightInject` from `test/Paramore.Darker.Tests/Paramore.Darker.Tests.csproj`
  - Remove `ProjectReference` to `Paramore.Darker.SimpleInjector` from `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`
  - Remove `ProjectReference` to `Paramore.Darker.LightInject` from `test/Paramore.Darker.Tests.AOT/Paramore.Darker.Tests.AOT.csproj`

- [ ] **2. Remove integration test files for SimpleInjector and LightInject**
  - Delete `test/Paramore.Darker.Tests/Integrations/SimpleInjectorTests.cs`
  - Delete `test/Paramore.Darker.Tests/Integrations/LightInjectTests.cs`

- [ ] **3. Remove projects from solution files**
  - Remove `Paramore.Darker.SimpleInjector` entry from `Darker.slnx`
  - Remove `Paramore.Darker.LightInject` entry from `Darker.slnx`
  - Remove `Paramore.Darker.SimpleInjector` entry from `Darker.Filter.slnf`
  - Remove `Paramore.Darker.LightInject` entry from `Darker.Filter.slnf`

- [ ] **4. Remove package versions from central package management**
  - Remove `SimpleInjector` entry from `Directory.Packages.props`
  - Remove `LightInject` entry from `Directory.Packages.props`

- [ ] **5. Delete source project directories**
  - Delete `src/Paramore.Darker.SimpleInjector/` directory and all contents
  - Delete `src/Paramore.Darker.LightInject/` directory and all contents

### Phase 2: Verify Build (Validation)

- [ ] **6. Verify solution builds and all tests pass**
  - Run `dotnet build Darker.Filter.slnf -c Release`
  - Run `dotnet test Darker.Filter.slnf -c Release --no-build`
  - All existing tests (except removed integration tests) must pass

### Phase 3: Update Documentation

- [ ] **7. Update README.md**
  - Remove references to SimpleInjector and LightInject packages
  - Note the breaking change and migration path to MS DI

- [ ] **8. Update CLAUDE.md**
  - Remove `Paramore.Darker.SimpleInjector` and `Paramore.Darker.LightInject` from the Key Components list
  - Update any other references

- [ ] **9. Update agent instruction files**
  - Update `.agent_instructions/project_structure.md` — remove references to the deleted packages
  - Update `.agent_instructions/code_style.md` — remove references if present
  - Update `.agent_instructions/testing.md` — remove references if present

- [ ] **10. Update ADR 0004 to reflect removal**
  - In `docs/adr/0004-factory-registry-abstractions.md`, update the "Container Integration Pattern" section to note that only the MS DI integration package is shipped
  - Remove the reference to `Paramore.Darker.SimpleInjector` as an example

- [ ] **11. Update ADR 0003 if it references the removed packages**
  - Check `docs/adr/0003-fluent-builder-for-query-processor.md` for references
  - Update or remove as needed

### Phase 4: Final Verification

- [ ] **12. Final build and test verification**
  - Run `dotnet build Darker.Filter.slnf -c Release` (clean build)
  - Run `dotnet test Darker.Filter.slnf -c Release --no-build`
  - Verify no remaining references: search codebase for `SimpleInjector` and `LightInject` (excluding ADR 0006, requirements, and tasks)
