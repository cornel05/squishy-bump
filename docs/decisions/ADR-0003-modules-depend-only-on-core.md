# ADR-0003: Modules depend only on Core

**Status:** accepted · **Date:** 15/11/2026 · **Refs:** GDD 10.3, 13.2

## Context
Two people (and their agents) build modules in parallel. Direct references between modules create
merge conflicts, hidden coupling and compile breaks when one side is half done.

## Decision
- Gameplay assemblies (`Physics`, `Hazards`, `AI`, `Match`, `Net`, `UI`, `Presentation`) reference
  `SquishyBump.Core` plus their own Unity packages, and nothing else from SquishyBump.
- `SquishyBump.Core` references no SquishyBump assembly.
- Cross-module communication uses Core interfaces and events; `SquishyBump.App` (composition root)
  is the only assembly that references every module and wires them.
- Namespace = assembly name (`rootNamespace` set in each asmdef). References by name, not GUID.
- An exception needs its own ADR containing a line `Allowed-Reference: SquishyBump.X -> SquishyBump.Y`.

## Enforcement
`tools/check_repo.py` (pre-commit, CI) and `EngineSettingsTests.Core_ReferencesNoOtherSquishyBumpAssembly`.

## Consequences
Stubs behind Core interfaces let each member work without waiting for the other (SB-06).
Core changes affect both members; the author tells the teammate (review is optional since 15/11/2026).
