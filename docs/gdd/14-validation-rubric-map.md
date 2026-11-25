# 14. Validation and rubric map

> **In this chapter:** How we prove the design works, and where each rubric criterion is answered.

## 14.1 Design consistency checks

| Claim | Calculation | Result |
|---|---|---|
| A fresh fighter cannot be rung out from the centre of the Training Ring by one bump | Worst case Light: 11.76² / (2 × 14.4) | 4.8 m < 12 m ✓ |
| Wobble makes finishing possible once the first ring has dropped | Normal at h = 5: 16² / (2 × 12); Light at h = 5: 18.8² / (2 × 14.4) | 10.7 m > 9 m; 12.3 m > 12 m ✓ |
| On ice one dash rings out a fresh Light fighter, but not a Heavy one | Light 11.76² / (2 × 7.2); Heavy 8.0² / (2 × 4.8) | 9.6 m > 9 m; 6.7 m < 9 m ✓ |
| Every match ends | L1: last ring drops at 135 s; L2–L5: time limit 150 s, survivors ranked by Wobble | ≤ 150 s ✓ |
| Every hazard telegraphs at least 1.0 s | H01 3 s; H02 2 s; H03 painted lanes, dock light 1.0 s; H04 1.0 s; H05 1.0 s + 1.0 s flight; H06 1.5 s; H07 boxes always visible, whoosh 1.3 s ahead | ✓ |
| Weight classes feel equally responsive | v_max / a_max = 0.47 / 0.50 / 0.52 s | Within 0.05 s ✓ |
| An obstacle box can be jumped and never rings out a fresh fighter from the inner lane | Jump apex 1.2 m > 0.8 m; boxes only occupy 5.4–6.4 m, so the 4–5.4 m lane is never touched | ✓ |
| Rank matches always end | L1: stops at 105 s, last drop 135 s; L2–L5: respawns run until the 150 s limit, which ends the match | Rank matches end ✓ |
| Network load fits LAN and relay | 200 B × 30 Hz × 5 clients | ≈ 30 KB/s ✓ |

These checks are repeated by a small editor test that reads the BalanceProfile, so a later tuning change that breaks one of them fails before a build is made.

## 14.2 External playtest plan

Two rounds with players outside the team: a vertical-slice round (L1–L3, four players on LAN) around the middle of implementation, and a full-game round (all levels, both rules, six players) before feature freeze. At least eight outside participants in total. Each session has a short unassisted play period, a 5-point questionnaire and a short interview. Telemetry logs match length, eliminations by cause, chosen fighter and class, win rate per class, Rank points per class, retries and network corrections.

| Metric | Initial target | Interpretation |
|---|---|---|
| Tutorial completion time | Median ≤ 90 s | Longer means unclear prompts or controls |
| Match length per level | Within ±15 s of the targets in 6.6 | Tune hazard timing, the time limit or knockback |
| Class balance | Each class wins 20–45% of matches over all levels; no class picked by more than 50% of players | Outside the band: tune v_max, traction or mass in ClassProfile |
| Hazard fairness | Median ≥ 4 of 5 | Low scores mean telegraphs are too short or unclear |
| Survival clear rate (first try) | L1 ≥ 80%, L3 40–60%, L5 15–35% | Tune bot tiers in Appendix A |
| Spawn seat balance | No seat wins more than 30% of 30 matches | Rotate or adjust spawn points |
| Fun | Median ≥ 4 of 5; ≥ 70% want another match | Core-loop validation |
| Network corrections | < 1 visible snap per player per minute on LAN | Tune buffer and reconciliation |

## 14.3 Priority acceptance tests

P0 tests define the MVP acceptance baseline for Assignment 02 (all five levels L1–L5, both rules, Single and LAN Multiplayer; Section 13.4); P1 tests cover the remaining full scope (relay, adverse network, rebinding) once P0 passes.

| ID | Prio. | Scenario | Pass condition |
|---|---|---|---|
| T01 | P0 | Run the balance editor test | All claims in 14.1 pass |
| T02 | P0 | Two fighters collide head-on at known speeds | Velocities after impact match the formula within 2% |
| T03 | P0 | Pick each of the six fighters in the lobby, play one match each, then try to change class mid-session | Mass, radius and v_max match the class table; a taken fighter cannot be picked twice; no class change is possible after the session starts |
| T04 | P0 | Full Single Survival run L1–L5 against bots | Retry keeps roster, bot fighters and seed; run summary after L5 |
| T05 | P0 | Full Single Rank run L1–L5 | Three lives; respawn after 2 s with 1.5 s invulnerability; +1 only to the last toucher within 3 s; respawns run until the time limit (L1: until 105 s); correct final ranking and ties |
| T06 | P0 | Six-player LAN session on separate PCs, L1–L5, Rank rule | No desync of lives, points or placements; all clients show the same result |
| T07 | P0 | Idle match with no input on every level, both rules | Ends by 150 s with a valid result; survivors ranked by Wobble |
| T08 | P0 | Two last fighters fall in the same tick | Draw; shared rank 1; Survival requires retry |
| T09 | P0 | Each hazard H01–H05 and H07 observed for 60 s (H06 under T10) | Telegraph precedes effect by the specified time on host and clients |
| T10 | P0 | Frost Ring 120 s with no fighters | Never below 37 tiles; largest region never split |
| T11 | P0 | Client disconnects mid-match | Eliminated at that tick; session continues; can rejoin lobby |
| T12 | P0 | Host quits | Clients return to menu with a message; no crash |
| T13 | P0 | Bot tests AI-1 to AI-6 in Appendix A.7 | All pass |
| T14 | P0 | Performance on the reference PC, Chaos Core, six fighters | 60 FPS median; p95 frame time ≤ 25 ms |
| T15 | P1 | LAN with 100 ms RTT and 2% loss (simulator) | Playable; local movement still immediate; no stuck fighters |
| T16 | P1 | Four players join by relay code from two networks | Complete session without errors |
| T17 | P1 | Rebind every action and restart the game | Bindings persist and work |
| T18 | P0 | Multiplayer Survival with 6 players over 5 levels and 4 players over 3 levels | Knockouts follow n_out per match; exactly one champion after the last level |
| T19 | P1 | Full Survival and Rank sessions over relay, then the single-player opening and ending cutscenes | Same pass conditions as T04–T06 and T18; cutscenes play and can be skipped |
| T20 | P0 | Training Ring with six idle bots, both rules | Rings drop at 45/75/105/135 s after a 3 s warning; fighters on a dropped ring fall; under Rank respawn points always lie on the remaining floor and respawns stop at 105 s |

## 14.4 Assignment 01 traceability

| Course constraint | Design evidence | Implementation gate |
|---|---|---|
| Physics implementations | Custom sphere-sphere impulse resolution, positional correction, traction-limited motion, rolling without slipping, ballistic solver, radial and cone forces, kinematic box contacts, surface velocity (6) | T01, T02, T03, T09, T10, T20 |
| AI module | FSM, utility targeting, danger map, grid A\*, five difficulty profiles, roster rule (6.7, Appendix A) | T04, T13 |
| Network | Host-authoritative 2–6 player sessions over LAN and relay, input prediction, interpolation, event design (10.4) | T06, T11, T12, T15, T16 |
| Modern game engine | Unity 6 LTS, URP, Netcode for GameObjects, ScriptableObject data (10.2, 10.3) | Windows build per README |

| Rubric criterion | Points | Evidence in this GDD |
|---|---|---|
| 1.1 Concept and vision | 15 | Chapters 1 and 2: genre, platform, audience, USPs, analysed references |
| 1.2 Core gameplay and mechanics | 20 | Chapter 3: loop diagram, controls, modes, win/lose and edge cases |
| 1.3 Game systems and balance | 15 | Sections 6, 4.2 and 14.1: balance table, formulas, worked examples, difficulty curve |
| 1.4 Level and content design | 15 | Sections 6.6, 9 and 7: five levels, one new mechanic each, hazard specifications |
| 1.5 Narrative, characters and world | 10 | Section 8 and Appendix B: setting, six characters, three acts, delivery and cutscene script |
| 1.6 Art, audio and UI/UX | 10 | Chapters 11 and 12: screen flow, HUD, menus, accessibility; visual direction, 3D characters, Creative palette, UX/UI screens and audio list |
| 1.7 Technical plan and production scope | 10 | Chapter 10 (engine, architecture, network) and Chapter 13 (asset list, MVP, milestones, roles) |
| 1.8 GDD quality and consistency | 5 | Shared IDs (F, O, PF, H, T, AI), definitions in 5.2, consistency checks in 14.1 |

## 14.5 Evidence for Assignment 02

The delivery includes a Windows build, the Unity project with the pinned editor version, a README (LAN setup, port 7777, relay join code, controls, known limits), asset credits with an AI-generation declaration, the change log against this GDD, the test record for T01–T20 and the playtest report. The demo shows a LAN session in the 3D lobby, a Heavy fighter holding against a cannon gust, a Rank respawn, a bot evading a cannon cone and a full match ending. Each member explains their own code from a steady Git history.
