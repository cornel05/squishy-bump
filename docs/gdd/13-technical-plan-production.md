# 13. Technical plan and production

> **In this chapter:** Engine, components, asset list, MVP and full scope, milestones and roles.

## 13.1 Engine and rationale

Squishy Bump is developed with Unity 6, C# and the Universal Render Pipeline (URP). The engine suits spherical characters, stylised toy environments and challenges built on motion, collision and push forces. Simple materials (a glossy jelly shader for the fighters) and moderate lighting keep the production volume under control.

| Choice | Role and reason |
|---|---|
| Unity 6 | Scenes, physics, animation, interface and test tools in one environment |
| C# | Character control, match rules, hazards, AI and interface |
| URP | Fits the low-poly art direction and scales display quality |
| Netcode for GameObjects, Unity Transport | State synchronisation between machines; Multiplayer over LAN |
| Multiplayer Services / Relay | Internet connection added once the LAN flow is stable |

## 13.2 Architecture and main components

Systems are organised by responsibility so that the team can build, integrate and test them independently. Single Player and Multiplayer share the movement, weight-class, collision and hazard logic, which keeps gameplay consistent. The component list, data ownership and message design are specified in Sections 10.3–10.4.

| Component | Main responsibility |
|---|---|
| SessionManager | Session, Single Player or Multiplayer, participants, chosen fighters, rule and session length |
| MatchDirector | Preparation, countdown, play and result states; tracks remaining fighters, lives and respawns, confirms eliminations and ends the match |
| InputRouter / PlayerMotor | Receives commands and performs movement, jump and dash |
| FighterProfileApplier | Applies the Heavy / Normal / Light profile of the chosen fighter at spawn; fixed for the session |
| CollisionResolver | Collisions, push forces and physical interaction with opponents and the environment |
| HazardDirector | Ring drops, turntable, obstacle ring, pusher blocks, cannons and vanishing tiles; warnings synchronised with impact |
| BotBrain / Sensors | Observe opponents, edges and hazards to drive the five Single Player bots through the same character controls |
| ScoreService | Results and session score under the Survival or Rank rule, including last-touch credit, in Single Player or Multiplayer |
| CameraRig / RollVisual | Camera and the rolling, jelly squash-and-stretch and expressions of the fighters |
| UI / AudioDirector / LobbyWorld | Menus, the walkable 3D lobby (fighter pads, host console, info board), HUD, results and sound feedback |

Authority in Multiplayer. The host runs the main gameplay state: physics, collisions, hazards, lives, eliminations and results. Clients send input and receive the confirmed state to update visuals, HUD and sound, so every machine shows the score confirmed by the host. Data. Fighter, weight-class, hazard and arena profiles are ScriptableObjects, keeping balance data out of code; all machines start a match with the same configuration. Scoring, end conditions and ties are defined in Sections 3.5–3.6; consistency of collisions and results is tested on six machines.

## 13.3 Asset list

Assets are produced by the team with AI image tools and procedural code, then adjusted to one cute toy style, a bright palette and clear readability in play. Every asset and its generator is declared in Appendix C; the current visual targets are in Chapter 12.

| Asset group | Content | Requirement |
|---|---|---|
| Characters | Six squishy animals in three sizes, shared face set, expressions and squash animation | Six fighters distinguishable by shape and colour; class readable by size; movement, jump and dash clearly shown |
| Environment | Five toy-store play areas, the 3D lobby aisle, moving floors, cannons, ice tiles and store decoration | Matches the layouts; never hides edges or warnings |
| UI | Main menu, Host/Join, rule select, 3D lobby panels (host console, info board), HUD and results | Readable; class badges and life hearts are clear at a glance |
| VFX | Impact, dash, ring drops, cannons, warnings, breaking floor, falls and respawn | Synchronised with impact; warnings use red flashing plus a clear shape |
| SFX | Rolling, squish impact, dash, cannons, ring and floor warnings, last-10-seconds tick, breaking floor, falls, respawn, win/lose and UI | Clear feedback paired with visual signals |
| Music | Menu, match and result music | Playful; never masks warning sounds |
| Story content | Opening and ending scenes and play-area cards | Follows the approved script; reuses existing characters and environments |

During the MVP the team uses placeholder assets to validate gameplay, then replaces them with final versions. Integrated assets must keep fighters, edges and warnings readable even when many effects play together.

## 13.4 MVP and full scope

The MVP is the acceptance baseline for Assignment 02: complete matches from the 3D lobby to the session result on all five levels, in Single Player and in LAN Multiplayer, under both session rules. The P0 tests of Section 14.3 define its acceptance.

| Area | MVP (required) | Done when |
|---|---|---|
| Arenas | L1 Training Ring to L5 Chaos Core with hazards H01– H07 and the 150 s time limit | Valid spawns and respawns; floor, edge and warnings readable; T09, T10, T20 pass |
| Controls and fighter classes | Move, jump, dash; six fighters with three class profiles fixed per session | Stable inputs; correct profile and HUD badge; T03 passes |
| Physics | Custom impulse collisions, Wobble knockback, traction, ice, cannon forces, surface motion and ring drops | T01–T03, T07–T10 pass |
| Single Player | One human and five bots D1–D5; Survival and Rank over L1–L5; tutorial | T04, T05, T13 pass |
| LAN Multiplayer | Two to six humans; 3D lobby with fighter pads and host console; ready and start; both rules; session length L1 to Lk | T06, T11, T12, T18 pass |
| Interface and feedback | Menus, 3D lobby, HUD, results; placeholder art and sound | Class, lives, fighters left, dash cooldown and time readable |

The full scope adds Internet play through Unity Relay, the opening and ending cutscenes and the final art and audio; it is accepted by the P1 tests. Gamepad support is a stretch goal. Local split-screen, host migration, ranked matchmaking, a skin shop, in-match class switching and stat upgrades are out of scope.

## 13.5 Milestones and roles

The plan fits the time actually left before the Assignment 02 deadline. The deadline moved one week later than first expected and is currently estimated at 17 November 2026, to be confirmed with the lecturer. Dates shift together if the deadline changes; the MVP milestones keep priority over the full scope.

| Milestone | Date | Main output | Owners | Exit condition |
|---|---|---|---|---|
| M1 Greybox | 10/11 | Training Ring with ring drops; move, jump, dash; six fighter profiles; collisions, ring-out | Đình Duy: gameplay logic. Minh Nhật: layout and placeholder UI | One match from countdown to result |
| M2 Network, lobby and rules | 11/11 | LAN 3D lobby for 2–6 players with fighter pads and host console; five basic bots; Survival and Rank (lives, respawn, last-touch points); HUD and results | Đình Duy: network, rules and bots. Minh Nhật: lobby scene, interface and integration | All machines agree on results; disconnect handled |
| M3 Hazards L2–L3 | 12/11 | L2 with reversing turntable, obstacle ring and pusher blocks; L3 with rim cannons; time limit and tie rule; tutorial | Đình Duy: hazards and physics. Minh Nhật: layouts, cards and test set-up | T09 passes for H01–H05 and H07 |
| M4 L4–L5 and playtest 1 | 14/11 | L4 Frost Ring and L5 Chaos Core with H06; bots D4–D5; external playtest round 1; fixes | Đình Duy: H06, bots and fixes. Minh Nhật: run and record the playtest | All five levels playable; blocking issues logged |
| M5 MVP freeze | 15/11 | All P0 tests; playtest round 2; Relay and cutscenes if time allows (P1) | Đình Duy: P0 fixes, Relay, assets. Minh Nhật: story content and playtest 2 | All P0 tests pass; P1 results recorded |
| M6 Submission | 17/11 | Priority fixes; build, README, credits, change log, test and playtest reports | Both; Đình Duy: build and asset manifest; Minh Nhật: documents and test log | Build runs from the README |

Coordination. Đình Duy leads mechanics, physics, networking, AI and asset generation; Minh Nhật leads layouts, interface, story content, playtests and document consolidation. Each milestone is reviewed by the other member before dependent work starts. Change control. If progress slips, the team protects the match flow and LAN play first and reduces decoration, animation or full-scope items. Any reduction of committed content is agreed and logged in Appendix D. LAN and Internet results are accepted separately.
