# SquishyBump.Physics

**Owner:** Đình Duy (`cornel05`)  
**Assembly / namespace:** `SquishyBump.Physics`

## Responsibility
Fighter movement and collisions: `PlayerMotor` (move, jump, dash), `CollisionResolver` + wobble + stun, `FighterProfileApplier`.

## Design refs
- GDD sections: 4.2, 4.4, 6.1–6.4 (`docs/gdd/`)
- IDs: PF01–PF08, O01 (look up in `docs/gdd/ids.md`)

## Invariants
- Host-authoritative: runs only on the host; clients never write positions or velocities.
- Reads only `InputCommand`, never the keyboard or `SquishyControls` directly.
- All tuning comes from BalanceProfile / ClassProfile assets (GDD 6.1). No magic numbers.
- Runs in `FixedUpdate` at 60 Hz.

## Public API surface
`PlayerMotor`, `CollisionResolver`, `FighterProfileApplier` (consumed via Core interfaces).

## Related issues
SB-10 #8, SB-11 #9, SB-12 #10, SB-19 #17, SB-28 #26

Root rules: `/AGENTS.md`. Architecture: `docs/architecture.md`.
