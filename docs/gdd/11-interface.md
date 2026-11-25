# 11. Interface

> **In this chapter:** Screen flow, HUD, menus, the 3D lobby and accessibility.

Drafted by Đình Duy from the mockups in Section 12.6, for review by Minh Nhật.

## 11.1 Screen flow

The interface follows the session flow of Section 5.3. From the main menu the player chooses Single Player or Multiplayer; Multiplayer then offers Host or Join. Single Player and Host then choose the rule (Survival or Rank) and enter the walkable 3D lobby in the toy store; a joining client enters the host's lobby directly. Every match goes through the arena card, the countdown, the HUD and the match result, and the last match leads to the session result and, in Single Player, the ending cutscene.

| Screen | Content | Exit |
|---|---|---|
| Main menu | Single Player, Multiplayer, Settings, Credits, Quit | Rule select, Host/Join, Settings, Credits |
| Multiplayer: Create / Find Server | Create Server opens a LAN room in the 3D lobby; Find Server opens `console_rooms.png` listing LAN rooms (`lobby_row.png`), with direct IP entry and optional room password | 3D lobby |
| Rule select | Two cards: Survival (one life, last fighter standing) and Rank (three lives, points per ring-out); best records | 3D lobby (opening cutscene first in Single) |
| 3D lobby (STAR CATCHER) | Walkable interior of the STAR CATCHER claw machine (`Lobby.glb`). Six floor pads with floating class cards (`card_heavy`, `card_normal`, `card_light`): step onto a pad to pick that animal (taken fighters are locked). Match Setup opens the Lobby Console (`console_lobby.png`) with Match Setup / Map tabs, Survival/Rank rule, length L1–Lk, capacity 2–6, Private/Public mode and password. A 5 s countdown starts once players are ready | Arena card |
| Arena card and countdown | Arena name, one-line challenge, mechanic icon (Appendix B.3); 3-2-1-GO | Match (HUD) |
| Match result | Podium top 3; Survival: knockouts; Rank: points gained and lives left; most Wobble | Next level, retry (Survival Single) or session result |
| Session result | Final ranking, champion, records | Ending cutscene (Single) or main menu |

*Figure 10: Screen flow*

## 11.2 HUD

The HUD keeps the centre of the screen free for the arena. Information sits in the four corners, and the only element allowed near the centre is a hazard banner, which never covers the floor around the player's fighter.

| Element | Position | Content and update rule |
|---|---|---|
| Lives or fighters left | Top left | Rank: the player's life hearts and the 2 s respawn countdown. Survival: fighters remaining / total, updated on each confirmed elimination |
| Match clock | Top centre | Time left out of 150 s (L1: time to the next ring drop); turns red in the last 10 s |
| Scoreboard | Top right | Level x/n, players with fighter colour; Rank: points and lives; Survival: alive or knocked out; eliminated players struck through |
| Hazard banner | Below the clock | Short amber text with icon for the next cannon, tile wave, ring drop or ring reversal; at most one at a time |
| Wobble counter | Above each fighter | ×h; turns red from 5 hits; a +1 pop appears over the credited fighter after a Rank ring-out |
| Fighter and class panel | Bottom left | 3D-rendered fighter avatar (`avatar_0.png`–`avatar_5.png`) and name with the class badge (for example Heavy · 15 kg); under Rank the session points and the last-touch reminder |
| Dash | Bottom left, inside the fighter panel | Icon with a 3 s cooldown ring and the remaining time |
| Key hints | Bottom right | Jump and dash keys; can be hidden in Settings |

Players who are out of the match switch to the spectator camera, which follows a surviving fighter and cycles with the left and right keys. A small "Spectating" label and the remaining fighters stay on screen. Under Rank, a fighter waiting to respawn keeps its own camera and sees the 2 s countdown instead.

## 11.3 Menus, lobby and results

All 2D menus use the same panel style (Section 12.3): rounded panels with a 5 px ink outline, buttons with a solid shadow, the primary action in amber and secondary actions in cream. Every 2D screen has a Back button in the bottom-left corner. The 3D lobby uses the same panel style for the host console and the info board, and Esc opens a Leave option. The info board shows connection details for LAN and relay so that a host can read them aloud during a demo, and connection errors are specific: wrong version, room full, host unreachable or host left.

The Esc menu pauses only in Single Player. In Multiplayer it opens an overlay with Settings and Leave while the match continues. Results screens show the outcome first and wait at least 3 s before the continue buttons appear, so nobody skips them by accident.

## 11.4 Accessibility and comfort

- Every warning pairs colour with a shape and a sound; the game is playable with sound off or in greyscale (test LD10).
- Fighters differ in shape and colour, and weight classes in size and badge, not only colour.
- All actions can be rebound; mouse sensitivity and camera distance are adjustable.
- Screen shake and hit-stop can be turned off; flashing stays at 2 Hz or below.
- Interface text has a size option and every cutscene line is subtitled.
- English and Vietnamese interface share one layout through the Baloo 2 font.

## 11.5 Input and feedback reference

Key bindings and action feedback are specified in Section 3.4 and the action priorities in Section 4.4. The interface reflects every host-confirmed event within one snapshot (about 33 ms): ring-outs, respawns, eliminations, cooldowns, warnings and results.
