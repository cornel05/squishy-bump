# SquishyBump.Core

**Owner:** Both (Đình Duy `cornel05` + Minh Nhật `ZteHahah`)  
**Assembly / namespace:** `SquishyBump.Core`

## Responsibility
Shared contracts only: `InputCommand`, events, interfaces, ScriptableObject types (BalanceProfile, ClassProfile, FruitDefinition, LevelDefinition, BotProfile), seeded RNG, generated input wrapper.

## Design refs
- GDD sections: 3.4, 6.1, 10.3–10.5 (`docs/gdd/`)
- IDs: O01–O12 (object register: the data types) (look up in `docs/gdd/ids.md`)

## Invariants
- **Changes to Core need review from BOTH members**, because every module and stub depends on it.
- No behaviour: types, interfaces, events, data. No MonoBehaviour gameplay logic.
- References no other SquishyBump assembly (enforced by `EngineSettingsTests` and `tools/check_repo.py`).
- `Input/SquishyControls.cs` is generated from `Settings/Input/SquishyControls.inputactions`. Edit the asset, never the .cs.
- Deterministic: RNG is seeded and owned by the host.

## Public API surface
`InputCommand`, `I*` service interfaces (`ISessionService`, `IHostClock`, `IGameEvents`, `IGameEventSink`, `IFighterMotor`), event structs, ScriptableObject profile types, `SquishyBump.Core.Input.SquishyControls`.

## Related issues
SB-02 #3, SB-04 #5, SB-05 #6, SB-06 #7, SB-07 #62

Root rules: `/AGENTS.md`. Architecture: `docs/architecture.md`.
