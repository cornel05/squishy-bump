# SquishyBump.Editor

**Owner:** Đình Duy (`cornel05`)  
**Assembly / namespace:** `SquishyBump.Editor`

## Responsibility
Editor-only tooling: `BuildScript` (Windows build), `ProjectScaffold` (scenes, build list, input wrapper), validators.

## Design refs
- GDD sections: 13.1, 14.5 (`docs/gdd/`)
- IDs: — (look up in `docs/gdd/ids.md`)

## Invariants
- Editor platform only; never referenced by runtime assemblies.
- Batchmode entry points must exit non-zero on failure.
- `ProjectScaffold.Run` stays idempotent.

## Public API surface
`SquishyBump.Editor.BuildScript.BuildWindows`, `SquishyBump.Editor.ProjectScaffold.Run`.

## Related issues
SB-01 #2, SB-02 #3, SB-60 #47

Root rules: `/AGENTS.md`. Architecture: `docs/architecture.md`.
