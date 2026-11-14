# SquishyBump.Match

**Owner:** Minh Nhật (`ZteHahah`)  
**Assembly / namespace:** `SquishyBump.Match`

## Responsibility
Match flow and rules: `MatchDirector`, Survival and Rank rules, `ScoreService`, knockout, respawn, single-player roster.

## Design refs
- GDD sections: 3.5, 3.6, 6.6 (`docs/gdd/`)
- IDs: F01–F09 (session stages) (look up in `docs/gdd/ids.md`)

## Invariants
- Match state lives on the host; clients receive match events.
- Rule values (round time, points) come from data, not code.
- Exposes results through Core events; never calls UI directly.

## Public API surface
`MatchDirector`, `ScoreService` behind Core interfaces; match events in Core.

## Related issues
SB-16 #14, SB-26 #24, SB-27 #25, SB-28 #26, SB-29 #27

Root rules: `/AGENTS.md`. Architecture: `docs/architecture.md`.
