# Appendix D. Change log

> **In this chapter:** Every design change with its date and scope.

## D.1 Update log

| Code | Date | Change | Scope |
|---|---|---|---|
| D01 | 30/10/2026 | First concept (then called Bump Dojo): 3D low-poly party brawler on PC for players aged 10+, Unity 6 | Concept, gameplay, technical plan |
| D02 | 30/10/2026 | Single Player with one human and five bots; Multiplayer over LAN/Internet | Rules, lobby, AI, tests |
| D03 | 31/10/2026 | Survival and Leaderboard proposed as Multiplayer scoring methods (superseded by D13) | Mode rules, scoring, UI |
| D04 | 31/10/2026 | C# and the Universal Render Pipeline | Section 13.1, project settings |
| D05 | 01/11/2026 | Component architecture, host authority, ScriptableObject configuration | Architecture, sync, data |
| D06 | 02/11/2026 | Roles of Đình Duy and Minh Nhật | Plan and contributions |
| D07 | 02/11/2026 | Assets recorded as "team assets" (refined by D16) | Section 13.3, Appendix C |
| D08 | 03/11/2026 | Asset list and provenance record format | Production, asset management |
| D09 | 03/11/2026 | L4 becomes Frost Ring: ice friction 0.3 combined with cracking tiles as one mechanic | Chapters 6, 7, 14, Appendix A |
| D10 | 03/11/2026 | GDD reorganised into Parts I–V with a document map; chapters renumbered | Whole document |
| D11 | 04/11/2026 | Ice tiles become hexagonal: 61 in L4, 42 in L5; L5 outer radius about 9 m | Chapters 7, 9, 12 |
| D12 | 04/11/2026 | AI-generated asset pack A01–A15 and Chapter 12 Art and audio | Chapter 12, Appendix C |
| D13 | 04/11/2026 | Survival and Leaderboard are session rules for both Single Player and Multiplayer; Multiplayer takes 2–6 players; knockout rule for Multiplayer Survival (Leaderboard later replaced by Rank, D20) | Chapters 1, 3, 9, 11, 13 |
| D14 | 04/11/2026 | MVP set to L1–L3, Single Player and LAN Multiplayer (superseded by D24) | Chapters 13, 14 |
| D15 | 04/11/2026 | L2 shuttles: amplitude 1.5 m, rest centre ±9.5 m, no overlap; fire cannon fallback target; result confirmed 0.5 s after the last elimination | Chapters 3, 7, 9 |
| D16 | 04/11/2026 | Every AI-generated or procedural asset is declared individually in Appendix C, as the rubric requires | Sections 13.3, 15.2, Appendix C |
| D17 | 04/11/2026 | Đình Duy and Minh Nhật parts merged into v0.5; English version produced from the same content | Whole document |
| D18 | 08/11/2026 | Creative-team brief: the game is renamed Squishy Bump; setting moves to a toy store; fighters become six squishy jelly fruits | Whole document, Chapters 8, 12 |
| D19 | 08/11/2026 | Stance Shift removed: the weight class (Heavy, Normal, Light, r 0.60 / 0.50 / 0.45 m) is fixed by the fruit picked in the lobby; USPs become Fruit weight classes and Living Arena | Chapters 1, 2, 3, 4, 6, 10, 11, Appendix A |
| D20 | 08/11/2026 | Rank replaces Leaderboard: 3 lives, respawn after 2 s with 1.5 s invulnerability, +1 point to the last toucher within 3 s, respawns stop at sudden death (changed by D29); Survival unchanged |  | Chapters 3, 4, 6, 10, 11, 14 |
| D21 | 08/11/2026 | Levels always start at L1 and run in order; Single Player always plays all five; Multiplayer host chooses L1 to Lk |  | Chapters 1, 3, 9, 11 |
| D22 | 08/11/2026 | L1 becomes a 12 m play mat with four ring drops (H07, renumbered H01 by D28) at 45/75/105/135 s; L2 gains a sweeper bar (H08, also a short arm on L5; removed by D28); L3 becomes a round 9 m floor with the four cannons on the rim |  | Chapters 6, 7, 9, 14 |
| D23 | 08/11/2026 | Walkable 3D lobby in the toy store with fruit display stands, a host desk and an info board; main menu Single Player / Multiplayer / Settings / Credits / Quit, then rule select |  | Chapters 10, 11, 12 |
| D24 | 08/11/2026 | A02 deadline moved one week (about 17/11/2026, to be confirmed); MVP becomes all five levels with both rules in Single Player and LAN Multiplayer; Relay and cutscenes stay in the full scope; milestones re-planned |  | Chapters 13, 14 |
| D25 | 09/11/2026 | Story moved to the toy store (Squishy Cup, Golden Squish) keeping Minh Nhật's three-act structure and character personalities; proofread by Minh Nhật |  | Chapter 8, Appendix B |
| D26 | 09/11/2026 | Visual asset set regenerated for Squishy Bump (fruits, toy store, new hazards, lobby and HUD mockups) |  | Chapter 12, Appendix C |
| D27 | 10/11/2026 | Version 1.0 (final for Assignment 01): regenerated asset set integrated; text-to-image results rejected and replaced by scripted Blender renders (Appendix C); HUD layout aligned with the mockups; Minh Nhật proofreads |  | Chapters 11, 12, Appendix C |
| D28 | 11/11/2026 | Review of the 08/11 brief with Đình Duy: the sweeper bar (H08) and the sudden-death arena shrink were team assumptions, not in the brief, and are removed; L2 keeps the reversing turntable with shuttles; the ring drop becomes H01 (H07 is reused for the obstacle ring in D30) |  | Chapters 1, 6, 7, 9, 11, 12, 14, Appendix A |
| D29 | 11/11/2026 | Time limit of 150 s on L2–L5 replaces the shrink as the no-soft-lock rule; survivors at the limit are ranked by Wobble, lowest first (Survival single-player advances only as sole first); under Rank respawns stay open until the limit (L1: until the third ring drop) | Chapters 3, 6, 9, 11, 14 |
| D30 | 11/11/2026 | Brief re-checked line by line with Đình Duy: L2 adds an outer ring that turns against the turntable and carries three obstacle boxes that push fruits off (H07; ring R 4–6.5 m, 15°/s, boxes 1.2 × 1.0 × 0.8 m, impulse 50 N·s, jumpable); the shuttles stay (changed by D32) |  | Chapters 6, 7, 9, 12, 14 |
| D31 | 11/11/2026 | L5 follows the brief (ring with cannons, falling tiles, ice): the turntable is removed; L5 is the 61-tile ice field of L4 with one air and one fire cannon, waves every 3.5 s and cannons every 4 s |  | Chapters 7, 8, 9, 12, Appendix B |
| D32 | 11/11/2026 | H03 changes from two side shuttle platforms to two pusher blocks (1.5 × 1.0 × 0.8 m, 3 m/s) that run back and forth on two east–west lanes across the L2 rings and push fruits off; a block waits at its dock until no H07 box will cross its lane | Chapters 6, 7, 9, 12, 14 |

## D.2 Version management

Each GDD version records its number, date and main changes. When a design decision changes, the related parts are reviewed so that rules, parameters, maps, interface and test criteria stay consistent. Đình Duy and Minh Nh ật both check changes to rules, production scope and architecture before they enter the merged version. Older versions are kept for comparison; the latest approved version is the basis for implementation. The log records design updates; feature completion is confirmed separately by a build version and test evidence.
