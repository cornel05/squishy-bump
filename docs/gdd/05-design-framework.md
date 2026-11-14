# 5. Design framework

> **In this chapter:** The rules we design by, the shared vocabulary and the numbered game flow.

## 5.1 Design pillars

Readable in one second. Every threat has a telegraph (glow, floor cone, blinking tile, flashing ring or edge) of at least 1.0 s. The three weight classes differ in size and silhouette, and every fruit has its own colour, so players can read an opponent's class at a glance.

Physics is the only weapon. No health, no attacks, no damage numbers. Mass, velocity and position decide everything, so the maths in Section 6 is the actual rule set.

One new idea per level. Each level adds exactly one mechanic, and Level 5 combines them. Difficulty also rises through bot skill (Section 6.6).

Fair for every seat. The host simulates everything, hazards use seeded schedules, and hazard selection does not favour any player.

Out of scope for the minimum product: power-up pickups, character abilities, in-match class switching, ragdolls, local split-screen, host migration, ranked matchmaking, mobile builds and monetisation.

## 5.2 Glossary

| Term | Definition |
|---|---|
| Level | One arena with its own layout and mechanic (L1 to L5). Changing level changes content. |
| Match | One play of a level, from countdown until one fruit remains. A match has no internal rounds; under Rank a fruit may respawn inside the same match. |
| Run / Session | The ordered list of levels from L1 played in one sitting. Single-player calls it a run; multiplayer calls it a session. |
| Fighter / fruit | Any of the six jelly-fruit characters, human- or bot-controlled. Physically a sphere. |
| Ring-out | Elimination by leaving the arena (Section 3.6). |
| Placement | Rank in a match by elimination order. Used by Survival knockout and by the result podium. |
| Weight class | Heavy, Normal or Light physical profile set by the chosen fruit and fixed for the session (Section 6.4). |
| Lives / respawn | Rank only: three lives per match; a fall costs one life and the fruit respawns after 2 s until the time limit (on L1 until the third ring drop). |
| Wobble (h) | Hit counter of a fighter in the current match. Raises the knockback it receives (Section 6.3). |
| Hit | A received impulse of at least 30 N·s from a fighter or a hazard. |
| Hazard | An arena element that acts on fighters by itself (H01 to H07). |
| Telegraph | The visible and audible warning before a hazard acts. |
| Survival | Session rule with one life per match: only the last fruit advances (Single) or the lowest-placed players are knocked out after each match (Multiplayer). |
| Rank | Session rule with three lives per match, respawns and ring-out points credited to the last toucher; points accumulate over the session. |
| Time limit | 150 s per match on L2–L5; survivors at the limit are ranked by Wobble, lowest first. L1 ends with its last ring drop at 135 s. |

## 5.3 Game flow and object register

*Figure 2: Game flow with numbered stages*

Figure 2 stages (transcribed from the figure image; the PDF has them only as a picture):

| Stage | Label in Figure 2 | Next (edge labels as in the figure) |
|---|---|---|
| F01 | Main menu | F02 |
| F02 | Single Player or Multiplayer? | Single → F03a; Multi → F02b |
| F02b | Host or Join | Host → F03a; Join: IP or code → F03b |
| F03a | Rule select Survival / Rank | F03b |
| F03b | 3D lobby fruit stands, host desk, length L1 to Lk (Multi) | Single → F03c; Multi: 2 to 6 ready → F04 |
| F03c | Opening cutscene | F04 |
| F04 | Level intro card + 3-2-1 countdown | F05 |
| F05 | Match play player control loop (O01 to O09) | fall, Rank, lives left → F05r; out of the match → F06; one fruit left → F07 |
| F05r | Respawn after 2 s | F05 |
| F06 | Spectate | F07 |
| F07 | Match result placement, points (O11) | next level in order → F04; Survival Single and player not last standing → F08; session complete → F09 |
| F08 | Retry same level | F04 |
| F09 | Session result + ending cutscene (Single) | F01 |

F01 to F09 are session stages. F05 contains the player control loop of Section 3.4. Survival failures return through F08; all other results continue to the next level or to F09. Objects O01 to O12 appear in the register below and properties PF01 to PF08 in Section 4.2.

| Object | Properties and permitted actions | Feedback and governing section |
|---|---|---|
| O01 Fighter | PF01–PF08. Move, jump, dash; fruit and weight class fixed at spawn. | Rolling, jelly squash, fruit colour; Chapter 4 |
| O02 Bot | A fighter driven by BotBrain with a BotProfile D1– D5. | Same visuals as players plus a tier badge; Appendix A |
| O03 Arena floor | Static collider with a floor material μ; on L1 split into four droppable rings. | Edge outline, ring bands; Section 9 and H01 |
| O04b Obstacle ring | Kinematic outer ring counter-rotating to O04, carrying three box colliders. | Amber stripe on each box, whoosh; H07 |
| O04 Turntable | Kinematic disc, ω and direction schedule. | Arrow decals, ramp sound; H02 |
| O05 Pusher block | Kinematic box on a straight lane, speed, dock wait and lane-clear check. | Painted lane, amber dock light, toy horn; H03 |
| O06 Air cannon | Sweep, telegraph, cone gust. | Glow and floor cone; H04 |
| O07 Fire cannon | Aim, ballistic fireball, blast, burning patch. | Landing marker; H05 |
| O08 Ice tile | Friction μ = 0.3. States: solid, cracking, fallen, respawning. | Frost sheen, crack lines and shake; H06 |
| O09 Bounds and kill plane | Elimination test, respawn point selection. | Edge outline, fall sound |
| O10 Match director | Phase, clock, alive list, lives. | Countdown, timer, lives, last-10-seconds warning |
| O11 Score service | Placements, Rank points, last-touch credit, ties. | Result screen; Section 3.6 |
| O12 Network session | Host, join, 3D lobby slots, fruit picks, version. | Lobby and errors; Section 10.4 |
