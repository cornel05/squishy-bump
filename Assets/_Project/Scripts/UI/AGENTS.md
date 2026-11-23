# SquishyBump.UI

**Owner:** Menus/Settings: Đình Duy (`cornel05`) · HUD, Lobby, Results, Tutorial: Minh Nhật (`ZteHahah`)  
**Assembly / namespace:** `SquishyBump.UI`

## Responsibility
uGUI + TextMeshPro screens: main menu, host/join, settings, HUD, 3D lobby, results, scoreboard (Tab), arena card, banners.

## Design refs
- GDD sections: 3.4, 11 (`docs/gdd/`)
- IDs: F01–F09 (screens per stage) (look up in `docs/gdd/ids.md`)

## Invariants
- UI only reads state from Core events/interfaces; it never changes gameplay state.
- Input comes from `SquishyControls` UI/Gameplay maps.
- Menu/Settings and HUD/Lobby/Results are separate subfolders per owner.

- Console art (`console_lobby`, `console_rooms`) is placed by pixel box of the sprite (`UiFactory.PxRect`); re-importing art with other dimensions needs the boxes in `LobbyConsole` and `MainMenu` updated (`UiKitTests` guards the sizes).

## Public API surface
Screen controllers listening to Core events.

## Related issues
SB-17 #15, SB-25 #23, SB-31 #29, SB-32 #30, SB-48 #39, SB-50 #41, SB-51 #42, SB-72 #54, SB-73 #55

Root rules: `/AGENTS.md`. Architecture: `docs/architecture.md`.
