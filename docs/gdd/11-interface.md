# 11. Interface

> **In this chapter:** Screen flow, HUD, menus, the 3D lobby and accessibility.

Drafted by Đình Duy from the mockups in Section 12.6, for review by Minh Nhật.

## 11.1 Screen flow

The interface follows the session flow of Section 5.3. From the main menu the player chooses Single Player or Multiplayer; Multiplayer then offers Host or Join. Single Player and Host then choose the rule (Survival or Rank) and enter the walkable 3D lobby in the toy store; a joining client enters the host's lobby directly. Every match goes through the arena card, the countdown, the HUD and the match result, and the last match leads to the session result and, in Single Player, the ending cutscene.

| Screen | Content | Exit |
|---|---|---|
| Main menu | Single Player, Multiplayer, Settings, Credits, Quit | Rule select, Host/Join, Settings, Credits |
| Multiplayer: Host or Join | Host creates a LAN room or a relay join code; Join enters an IP address or a join code | Rule select (Host) or lobby (Join) |
| Rule select | Two cards: Survival (one life, last fruit standing) and Rank (three lives, points per ring-out); best records | 3D lobby (opening cutscene first in Single) |
| 3D lobby | Walkable store aisle. Six fruit display stands: walk into a stand to pick that fruit (taken fruits are locked). Host desk (cash counter): the host opens the settings panel for the rule and, in Multiplayer, the session length L1 to Lk. Info board: first level, players, chosen fruits and ready state, host IP and join code; a 5 s countdown starts when 2–6 players are ready. In Single, bots take the remaining fruits on Start | Arena card |
| Arena card and countdown | Arena name, one-line challenge, mechanic icon (Appendix B.3); 3-2-1-GO | Match (HUD) |
| Match result | Podium top 3; Survival: knockouts; Rank: points gained and lives left; most Wobble | Next level, retry (Survival Single) or session result |
| Session result | Final ranking, champion, records | Ending cutscene (Single) or main menu |

*Figure 10: Screen flow*

## 11.2 HUD

The HUD keeps the centre of the screen free for the arena. Information sits in the four corners, and the only element allowed near the centre is a hazard banner, which never covers the floor around the player's fighter.

| Element | Position | Content and update rule |
|---|---|---|
| Lives or fruits left | Top left | Rank: the player's life hearts and the 2 s respawn countdown. Survival: fruits remaining / total, updated on each confirmed elimination |
| Match clock | Top centre | Time left out of 150 s (L1: time to the next ring drop); turns red in the last 10 s |
| Scoreboard | Top right | Level x/n, players with fruit colour; Rank: points and lives; Survival: alive or knocked out; eliminated players struck through |
| Hazard banner | Below the clock | Short amber text with icon for the next cannon, tile wave, ring drop or ring reversal; at most one at a time |
| Wobble counter | Above each fruit | ×h; turns red from 5 hits; a +1 pop appears over the credited fruit after a Rank ring-out |
| Fruit and class panel | Bottom left | Fruit portrait and name with the class badge (for example Heavy · 15 kg); under Rank the session points and the last-touch reminder |
| Dash | Bottom left, inside the fruit panel | Icon with a 3 s cooldown ring and the remaining time |
| Key hints | Bottom right | Jump and dash keys; can be hidden in Settings |

Players who are out of the match switch to the spectator camera, which follows a surviving fruit and cycles with the left and right keys. A small "Spectating" label and the remaining fruits stay on screen. Under Rank, a fruit waiting to respawn keeps its own camera and sees the 2 s countdown instead.

## 11.3 Menus, lobby and results

All 2D menus use the same panel style (Section 12.3): rounded panels with a 5 px ink outline, buttons with a solid shadow, the primary action in amber and secondary actions in cream. Every 2D screen has a Back button in the bottom-left corner. The 3D lobby uses the same panel style for the host desk and the info board, and Esc opens a Leave option. The info board shows connection details for LAN and relay so that a host can read them aloud during a demo, and connection errors are specific: wrong version, room full, host unreachable or host left.

The Esc menu pauses only in Single Player. In Multiplayer it opens an overlay with Settings and Leave while the match continues. Results screens show the outcome first and wait at least 3 s before the continue buttons appear, so nobody skips them by accident.

## 11.4 Accessibility and comfort

- Every warning pairs colour with a shape and a sound; the game is playable with sound off or in greyscale (test LD10).
- Fruits differ in shape and colour, and weight classes in size and badge, not only colour.
- All actions can be rebound; mouse sensitivity and camera distance are adjustable.
- Screen shake and hit-stop can be turned off; flashing stays at 2 Hz or below.
- Interface text has a size option and every cutscene line is subtitled.
- English and Vietnamese interface share one layout through the Baloo 2 font.

## 11.5 Input and feedback reference

Key bindings and action feedback are specified in Section 3.4 and the action priorities in Section 4.4. The interface reflects every host-confirmed event within one snapshot (about 33 ms): ring-outs, respawns, eliminations, cooldowns, warnings and results.
