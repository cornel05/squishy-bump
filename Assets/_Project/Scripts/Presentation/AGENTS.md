# SquishyBump.Presentation

**Owner:** Đình Duy (`cornel05`)  
**Assembly / namespace:** `SquishyBump.Presentation`

## Responsibility
Visual and audio feedback: `RollVisual`, `CameraRig` (Cinemachine 3), `AudioDirector`, VFX/SFX telegraphs.

## Design refs
- GDD sections: 12 (`docs/gdd/`)
- IDs: B01–B10 (cutscenes, with SB-71) (look up in `docs/gdd/ids.md`)

## Invariants
- Pure presentation: reads replicated state, never writes gameplay state.
- Runs on every peer, including clients.
- Audio/VFX assets over 20 MB need approval (LFS quota).

## Public API surface
`RollVisual`, `CameraRig`, `AudioDirector`.

## Related issues
SB-13 #11, SB-49 #40

Root rules: `/AGENTS.md`. Architecture: `docs/architecture.md`.
