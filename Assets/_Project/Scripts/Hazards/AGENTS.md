# SquishyBump.Hazards

**Owner:** Đình Duy (`cornel05`) for H01–H05, H07 · Minh Nhật (`ZteHahah`) for `IceTiles/` (H06)  
**Assembly / namespace:** `SquishyBump.Hazards`

## Responsibility
Arena hazards: ring drop, turntable, pusher blocks, air cannon, fire cannon, obstacle ring, ice tiles.

## Design refs
- GDD sections: 6.5, 7, 12.5 (`docs/gdd/`)
- IDs: H01–H07, LV01–LV05 (look up in `docs/gdd/ids.md`)

## Invariants
- Hazard state and timing are decided on the host; clients only play visuals from hazard events.
- Every hazard telegraphs before it acts (warning time from data).
- Timings and forces come from ScriptableObjects; seeded RNG from Core.
- `IceTiles/` is Nhật's folder: only its owner edits it.

## Public API surface
`IHazard` implementations + hazard events defined in Core.

## Related issues
SB-15 #13, SB-40 #31, SB-41 #32, SB-42 #33, SB-43 #34, SB-44 #35 (H06)

Root rules: `/AGENTS.md`. Architecture: `docs/architecture.md`.
