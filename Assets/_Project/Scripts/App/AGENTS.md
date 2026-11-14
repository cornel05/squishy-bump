# SquishyBump.App

**Owner:** Both  
**Assembly / namespace:** `SquishyBump.App`

## Responsibility
Composition root: the ONLY assembly that references every module. Boot scene wiring, service registration, scene flow.

## Design refs
- GDD sections: 10.2, 10.3, 13.2 (`docs/gdd/`)
- IDs: — (look up in `docs/gdd/ids.md`)

## Invariants
- Wires modules together through Core interfaces. Contains no gameplay rules.
- New module references here are fine; module→module references are not (ADR-0003).
- Starts NGO in host mode for Single Player too (ADR-0002).

## Public API surface
Bootstrap MonoBehaviour(s) in `Boot` scene.

## Related issues
SB-06 #7, SB-26 #24

Root rules: `/AGENTS.md`. Architecture: `docs/architecture.md`.
