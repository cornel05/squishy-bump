# SquishyBump.AI

**Owner:** Minh Nhật (`ZteHahah`)  
**Assembly / namespace:** `SquishyBump.AI`

## Responsibility
Bots: `BotBrain` FSM, danger map, grid A*, utility targeting, difficulty D1–D5.

## Design refs
- GDD sections: 6.7, Appendix A (`docs/gdd/`)
- IDs: AI-1–AI-6 (look up in `docs/gdd/ids.md`)

## Invariants
- Bots run on the host and produce `InputCommand`s exactly like a human player. No direct physics writes.
- Bot tuning comes from BotProfile assets.
- Pathing reads level data through Core interfaces, never scene singletons.

## Public API surface
`BotBrain` (implements an input source interface from Core).

## Related issues
SB-05 #6, SB-18 #16, SB-46 #37, SB-47 #38

Root rules: `/AGENTS.md`. Architecture: `docs/architecture.md`.
