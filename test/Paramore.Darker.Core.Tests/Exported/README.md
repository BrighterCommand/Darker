# `Exported/` — public test doubles discovered by scanning

This directory holds the **public** test doubles that exist to be discovered by
assembly/handler **scanning** (for example `AddHandlersFromAssemblies`, which uses
reflection to find handler types in an assembly). Because these types are located
by reflection over the assembly — not referenced directly by the tests that rely on
them — their **names, namespaces, and accessibility are part of the test contract**.

## How this differs from `TestDoubles/`

| Directory      | Role                                                | Used how                                              |
|----------------|-----------------------------------------------------|-------------------------------------------------------|
| `Exported/`    | Public handlers/queries **discovered by scanning**  | Found by reflection (e.g. `AddHandlersFromAssemblies`) |
| `TestDoubles/` | Internal doubles a test **news-up and hands in**    | Referenced directly by the test that uses them         |

The doubles in `TestDoubles/` are internal implementation details of individual
tests. The doubles here in `Exported/` are, in effect, a fixture that scanning-based
tests reflect over.

## Do not rename or remove without checking scanning tests

Renaming, moving, or removing a type in this directory — or changing its
accessibility — can silently break scanning-based tests, which discover these types
by reflection rather than by a compile-time reference. Before changing anything here,
check the tests that scan this assembly (search for `AddHandlersFromAssemblies` and
similar reflection-based registration).

When a `TestDoubles/` double resembles one of these `Exported/` types, it is **copied
and renamed** to a distinct simple name — never reused under a different namespace —
so the scannable-public role and the internal-double role stay clearly separated.
