# 9. Levels and content

> **In this chapter:** Scaled layouts, spawns, pacing, tutorial and validation of the five arenas.

## 9.1 Overview and spatial organisation

Each level is a separate arena used for one ring-out match. Players choose positions and paths to approach opponents, avoid danger and stay on the floor.

Single Player and Multiplayer use the same layouts. Single Player has one human and five bots; Multiplayer has two to six humans. Both use the Survival or Rank session rule defined in Section 3.5, and levels are always played in order from L1; this chapter focuses on the space and challenge of each arena. The five arenas introduce and then combine the mechanics one by one.

| Level | Design role | Key challenge |
|---|---|---|
| L1 Training Ring | Learn controls, collisions and watching the floor | Four-ring tropical island in an inflatable pool whose outer rings drop away, H01 (Section 7.1) · visual scale 0.95× |
| L2 Moving Ground | Adjust position while the floor moves | Spinning circus music-box turntable H02, counter-rotating obstacle ring H07 and pusher blocks H03 (Section 7.2) · visual scale 0.80× |
| L3 Cannon Court | Watch opponents and environmental danger at once | Toy fishing-game board with air cannons H04 and fire cannons H05 around the rim (Section 7.3) · visual scale 0.60× |
| L4 Frost Ring | Control slides and leave floor that is about to vanish | Christmas snow-village frozen pond of 61 cracking hex ice tiles H06 (Section 7.4) · visual scale 0.90× |
| L5 Chaos Core | Combine every skill introduced so far | LEGO brick castle courtyard combining the 61 ice tiles H06 with rim air and fire cannons (Section 7.5) · visual scale 0.85× |

**Layout principles**

The floor must leave enough room to observe, change direction and choose an escape. The six spawn points must be separate, on valid floor and never inside an active hazard when the match starts. Each spawn's distance to the centre, the edge and the hazards is compared to detect positional advantage. Symmetric layouts are the starting point; fairness is then measured through the tests in Section 9.6.

The centre is where fighters compete for position, while areas near the edge raise the risk of being pushed off. Danger changes with opponent positions, hazard activity and, on L1, the ring drops (Section 7.1). Store decoration and cannon bases are placed so that they never block movement, create pinch points or hide a warning. From every spawn, the camera must let the player read the floor, the edge and nearby hazards, even when many fighters stand close together.

**Map conventions**

The maps are top-down plans with x horizontal, z vertical and the main floor at y = 0. Points 1–6 mark the six spawns. Spawning must account for the collider radius of the fighter's weight class (Section 6.1) so that fighters never overlap the floor or each other. Every map uses a 1 m grid, a 5 m scale bar and a shared legend: play-mat or wooden floor, the L1 ring bands, turntable, the L2 obstacle ring and its boxes, the H03 pusher lanes with their docks, light-blue ice tiles, a red tile with cracks as an examp le warning, amber cannon cones and fireball landing spots. Anything outside the floor is a fall zone.

Direction markers must be distinguishable from imminent-danger signals, which combine red flashing with a shape and a sound. Danger areas on the maps only illustrate placement; tile selection, cannon targets and random schedules follow Section 10.5.

## 9.2 Detailed arena design

All arenas share the movement, collision and ring-out rules. A fighter that falls off is out of the match under Survival, or loses one of its three lives and respawns under Rank (Section 3.6). The H01 ring drops end L1, and the 150 s time limit ends L2–L5 (Sections 3.6 and 7.1). No arena grants bonus points for standing in an area or taking a particular route.

### 9.2.1 L1 Training Ring

Design goal. A wide, simple space where newcomers learn controls, collisions and the distance to the edge, then learn to watch the floor itself. Until the first ring drops at 45 s, pressure comes mainly from other fighters, so players feel how direction and position matter.

Layout and spawns. A circular tropical island of radius 12 m set inside an inflatable pool (`Map1Demo.glb`), with no wall to stop fighters falling. It is split into four concentric 3 m rings (`Vong1.glb`–`Vong4.glb`: 12–9, 9–6, 6–3 and 3–0 m); each ring is one H01 drop. The six spawns sit on a circle of radius 6 m, 60° apart, on the inner edge of the second band, so the first drop never removes a spawn. Three round pool floats (`PhaoTron1.glb`–`PhaoTron3.glb`) and three unicorn floats (`PhaoUnicorn1.glb`–`PhaoUnicorn3.glb`) bob in the surrounding water alongside decorative palms and chests; decorations never touch the play area or hide the edge. Fighter visual scale on L1 is 0.95× (physics collider unchanged).

*Figure 4: Training Ring layout*

Movement and choices. The open centre lets players approach from any direction. Near the edge they must weigh the chance to push an opponent off against the room they need to brake after their own dash, and from 42 s they must also leave the band that is about to drop. The ring drops pull everyone toward the centre, so the match tightens without any extra rule.

Pacing and feedback. The Single Player tutorial happens before the real match (Section 9.3). After GO, all six fighters play under the same conditions. A contrasting edge, the flashing seam of the next band, impact effects and the fall transition must show players why they fell .

Specific tests. Observe whether players tell jump from dash, read the edge and the ring warnings, and choose a curved route. Record position and input before the first fall and whether fighters were on a dropping band. Under Rank, check that every respawn point lies on the remaining floor. The tutorial training toy appears only in the exercise and never counts as one of the six fighters.

### 9.2.2 L2 Moving Ground

Design goal. Introduce how a moving floor affects holding position. Players fight on two rings that turn against each other: the turntable in the middle and an outer ring whose obstacle boxes push careless fighters off. They can step between the rings, must watch two pusher blocks that race straight across both rings, and must keep their balance when the rings reverse.

Layout and spawns. A round floor of radius 6.5 m set inside an open circus music box on a toy-room wooden floor (`L2.glb`), surrounded by a rocket, race car, dinosaur plushies, lamb, teddy bear and beach ball. The central turntable (radius 4 m) and counter-rotating outer ring (4 to 6.5 m) carry three obstacle boxes; two painted east–west pusher lanes cross at z = +1.2 and −1.2 m with off-floor docks. The six spawns lie on the outer ring at radius 5.25 m, at least 2 m from every box and more than 0.3 m from both lanes at the start. Fighter visual scale on L2 is 0.80× (physics collider unchanged).

*Figure 5: Moving Ground layout*

Movement and paths. The turntable H02, the obstacle ring H07 and the pusher blocks H03 use the parameters of Section 7.2: the turntable spins at 20°/s and reverses every 15 s with a 2 s ramp, and the outer ring always turns the other way at 15°/s. The two pusher blocks run at 3 m/s along lanes 2.4 m apart, waiting at their docks for at least 2 s and until no H07 box is about to cross their lane, so a block and a box never meet.

Choices and feedback. The turntable opens new approach angles but makes holding position and braking harder, and each reversal throws fighters near its rim outward; a Heavy fighter rides a reversal best, a Light fighter is thrown furthest. On the outer ring a box passes any point every 8 s; players jump them, step into the box-free inner lane or cross onto the turntable, where the opposite rotation makes the step itself a small risk. The two pusher blocks cross every few seconds in opposite directions; players jump them or step off the painted lane, and the centre of the turntable, where both lanes pass, is never a safe place to wait. Arrows on the disc show its direction and turn amber 2 s before reversal; the lanes are always painted and each dock light turns amber 1 s before a block departs.

Specific tests. Run full movement cycles with all three weight classes and all bot tiers. Measure the outward slide at each reversal and every box and block contact (impulse, Wobble, re-hit lockout), and check that a block never departs into an H07 box. A valid standing position must never be eliminated by mistake. Stuck bots are checked against AI-6 in Appendix A.7.

### 9.2.3 L3 Cannon Court

Design goal. Make players watch both opponents and environmental danger. The floor stays flat to focus on two challenges: a directional gust and a radial blast.

Layout and spawns. A round floor of radius 9 m built on a toy fishing-game board over blue interlocking foam tiles (`Map3.glb`), framed by plush sea toys (purple octopus, orange fish, yellow starfish, sandcastles and toy fishing rods). The six spawns sit on a circle of radius 5.5 m. Four cannons stand just outside the rim at radius 9.9 m: fire cannons at 45° and 225°, air cannons at 135° and 315°, initially aimed at the centre. Decorative props have colliders disabled so they never block movement. Fighter visual scale on L3 is 0.60× (physics collider unchanged).

*Figure 6: Cannon Court layout*

Hazard operation. Air cannons H04 and fire cannons H05 follow Section 7.3: each sweeps ±60° at 30°/s and fires every 5 s, and the four are staggered by 1.25 s to spread the danger. An air cannon glows and shows a 15° half-

angle cone before it fires; a fire cannon marks the landing spot before the blast, and the landing marker and the later burning patch must look different.

Choices and feedback. Players can leave the cone to avoid a gust; a Heavy fighter is pushed less and can sometimes hold its ground. Before a blast they must find a way out of the radius; Light fighters move away fastest but still fly far if hit. Because the cannons sit on the rim, a fighter near the edge is closest to the danger and a fighter in the centre is reachable by all four, so neither is a permanently safe area. Every shot, including the first of the match, is clearly telegraphed.

Specific tests. Check the warnings from all six spawns and when fighters hide part of the floor. Check the 10–16 m targeting range of H05 and its fallback to a floor point 13 m along the sweep line when no fighter is in range (Section 7.3).

### 9.2.4 L4 Frost Ring

Design goal. Combine slippery ice with the risk of losing floor. Players must leave braking room, read tiles that are about to break and keep an escape route while chasing.

Layout and spawns. A recessed frozen pond inside a Christmas snow-village diorama (`map 4.glb`) surrounded by snowy pine trees, a snowman, a decorated Christmas tree with gifts, gingerbread figure, candy canes and a striped wishing well. The floor consists of 61 hexagonal ice tiles (`cuc bang 2.glb`), 2 m flat-to-flat, arranged as a centre tile plus four rings (1 + 6 + 12 + 18 + 24) with an outer radius of about 9 m (Section 7.4). The six spawns sit on a circle of radius 6 m, 60° apart. Fighter visual scale on L4 is 0.90× (physics collider unchanged).

*Figure 7: Frost Ring layout*

Hazard operation. Ice friction is 0.3 against 0.6 on the standard floor (Sections 6.1–6.2). The H06 rules of Section 7.4 apply: a group of tiles is chosen every 3 s (2 tiles in 0–30 s, 3 in 30–60 s, 4 after 60 s); a chosen tile shows white cracks, flashes red and shakes for 1.5 s before falling, and returns after 10 s with a 1 s fade-in. At least 37 tiles always stay solid and the largest solid region stays connected on the 6-neighbour tile graph.

Choices and feedback. Players can brake early, change direction or go around a cracked tile. Ice must look clearly different from standard floor, but its shine must never hide the cracks and the red warning. A respawning tile must look different from one that can already be stood on.

Specific tests. Measure slide distances by weight class and direction. Check that players read tile states and escape routes, and check bot behaviour on ice against Appendix A.4–A.5.

### 9.2.5 L5 Chaos Core

Design goal. Combine the challenges of L3 and L4: cannons around the rim over a floor of slippery, vanishing ice tiles. Players must choose position and escape routes while several dangers act at once.

Layout and spawns. Following Section 7.5, the arena sits inside a colourful LEGO-style brick castle courtyard (`L5.glb`) with yellow, orange and blue battlements, a red drawbridge, toy knight and corner towers, using the full 61-tile hexagonal ice field of L4 (2 m flat-to-flat, outer radius about 9 m, friction 0.3). The six spawns sit on a circle of radius 6 m, each inside a tile. One air cannon in the west and one fire cannon in the east stand just outside the rim at x = ±9.9 m. Fighter visual scale on L5 is 0.85× (physics collider unchanged).

*Figure 8: Chaos Core layout*

Movement and choices. Everything slides as on L4, but now a gust or a blast can arrive while the player is already braking on ice. Standing between the two cannons is the most exposed spot; the north and south edges are out of the cones but closer to the falling tiles. Light fighters escape a marker fastest, Heavy fighters slide least in a gust.

Operation and feedback. Cannons fire every 4 s; tile waves come every 3.5 s, choosing 2 tiles and 3 after 45 s (Section 7.5). When several warnings overlap, the cone, the blast ring and the cracks must each stay recognisable, and a new warning must never hide one that is still active. At least 37 of the 61 tiles always stay solid, using the same connectivity rule as L4.

Specific tests. Check that players read cracks and cannon cones at once and find an escape when danger zones overlap. Bot paths are checked against Appendix A.4 and in real motion.

## 9.3 Tutorial and first match

The tutorial takes place on the Training Ring in the first Single Player run (Section 3.7). Players learn each action before the five bots join. Each prompt advances once the action is performed; experienced players can skip. Tutorial time is separate from match time and triggers neither ring drops nor scoring.

| Step | Tutorial situation | Expected feedback | Completion |
|---|---|---|---|
| Move | Roll to a marker on the floor | The fighter rolls in the input direction; the marker lights up when reached | Marker reached |
| Jump | Jump in an open area | The fighter bounces up with landing feedback | One jump and landing |
| Dash | Dash toward a marker | Trail and cooldown counter appear | One valid dash |
| Know your class | A card shows the chosen fighter, its class badge and one sentence (for example: Light, fast but flies far) | Class icon matches the HUD badge | Card dismissed |
| Push | Bump a training toy placed near the edge | The toy is pushed off and falls | Toy pushed out; ring-out understood |

Markers and the training toy are tutorial props. A player who falls during the exercise is reset to the training point to continue the current step, with no rank or score. When the tutorial is finished or skipped, the props are removed and positions, cooldowns and Wobble are reset so all six fighters start equal. A short controls card and the countdown follow. The D1 bot setup for L1 is given in Section 6.6.

In the first match, players are meant to try approaching opponents, watch the collision feedback and discover how much the distance to the edge matters. They choose their own paths; no fixed sequence of actions is required. In Multiplayer, the 3D lobby has a controls board players can walk up to and shows the selected rule; the match starts when two to six players have each picked a fighter and are ready. Before L2–L5, an arena card briefly states the main challenge. Tutorials never pause a single player during a Multiplayer match, and text, icons and warnings stay readable with sound off and never cover the floor.

## 9.4 Progression and challenge pacing

The five arenas teach the challenges step by step: L1 focuses on controls, collisions and watching the floor as rings drop, L2 adds turning, reversing floors and pushing obstacles, L3 asks players to read cannon direction and timing, L4 tests slide control and reading breaking tiles, and L5 combines everything. Levels are always played in this order; a shorter Multiplayer session simply stops at Lk, so every session starts with the tutorial-friendly L1.

| Level | Environmental pressure | Bot tiers (Single) | Target median match |
|---|---|---|---|
| L1 | Wide arena island; the outer ring drops at 45, 75, 105 and 135 s | D1 | 60 s |
| L2 | Rings reverse every 15 s; a box passes each point every 8 s; pusher blocks cross every 8–12 s | D1–D2 | 70 s |
| L3 | Four cannons fire every 5 s, staggered by 1.25 s | D1–D3 | 75 s |
| L4 | Tile waves every 3 s; 2, then 3, then 4 tiles | D2–D4 | 75 s |
| L5 | Cannons every 4 s; tile waves every 3.5 s, more tiles after 45 s | D3–D5 | 85 s |

Bot tiers and target durations come from Section 6.6 and hazard parameters from Sections 7.2–7.5; they are starting values to verify, not measured results. Bot tiers apply only to Single Player; Multiplayer always has two to six humans and no bots.

Pacing within a match. The countdown lets players read their spawn, opponents and the layout; actions start only after GO. Early in the match there is room to approach and find a position, but hazards run on their own schedule, so this phase is not guaranteed safe. As the match goes on, collisions and Wobble raise the pressure; L4 adds tiles at 30 and 60 s and L5 at 45 s. On L1 the climax comes from the ring drops at 45, 75, 105 and 135 s. On L2–L5 there is no shrinking floor: the clock counts down from 150 s, turns red in the last 10 s, and survivors at the limit are ranked by Wobble, so a careful player can still win without a final push (Section 3.6). Under Rank, respawns stay open until the time limit (on L1 until the third ring drop at 105 s). All times are measured from GO and exclude intros, tutorials and Single Player pauses.

After a match. The game shows the result and a short pause before continuing. Single Player summarises the match against the five bots; Multiplayer applies the selected Survival or Rank rule. Advancing, retrying and ending a session follow Sections 3.5–3.6; level design adds no bonus or extra advance condition.

## 9.5 Content inventory

This list defines the content to design and how often it appears in each arena. Shared parts are reused across levels, so the number of appearances is not the number of models to build.

| ID | Content | Expected volume | Used in |
|---|---|---|---|
| LV01–LV05 | Arena layouts and configurations | 5 layouts, each with spawns and hazard settings | L1–L5 |
| CH01 | Six squishy animal fighters | Six 3D `.glb` models (Bear, Pig, Cat, Penguin, Chick, Frog) + six 3D-rendered 2D avatars (`avatar_0.png`–`avatar_5.png`), upright squash rig, per-map visual scale (Sections 6.1, 8.1, 12.2) | All arenas and the lobby |
| TR01 | Tutorial training toy and markers | One training toy and a marker set, tutorial only | Tutorial on L1 |
| ENV01 | Static floors, L1 four island rings, pool floats and Creative backdrops | L1 `Map1Demo.glb` + `Vong1`–`4.glb` + `PhaoTron1`–`3.glb` + `PhaoUnicorn1`–`3.glb`; L2 `L2.glb`; L3 `Map3.glb` | L1–L3 |
| ENV02 | Turntable H02 | One turntable | L2 |
| ENV07 | Obstacle ring H07 | One counter-rotating ring with three toy-block obstacle boxes | L2 |
| ENV03 | Pusher blocks and docks H03 | Two blocks, two painted lanes and two docks | L2 |
| LOB01 | STAR CATCHER 3D lobby | Walkable STAR CATCHER claw machine (`Lobby.glb`) with six fighter floor pads, three floating class cards (`card_heavy/normal/light.png`), Match Setup console and Find Server browser | Lobby |
| ENV04 | Air cannon H04 | One type; two in L3, one in L5 | L3, L5 |
| ENV05 | Fire cannon H05 | One type; two in L3, one in L5 | L3, L5 |
| ENV06 | L4 snow-village pond, L5 brick castle and hexagonal ice tile H06 | Environment `map 4.glb` (L4), `L5.glb` (L5) and ice-tile mesh `cuc bang 2.glb` (61 tiles in L4 and 61 in L5) | L4, L5 |
| HUD01 | Arena introduction cards | Five cards in one shared layout | Before each level |
| FX01 | Effects and warning marks | Gust cone, blast spot, cracks, direction arrows, ring seams, respawn sparkle, last-10-seconds clock | H01–H07 |
| AUD01 | Ambient and hazard sound | Warning, cannon, breaking floor and motion sound groups | L1–L5 |

L4 and L5 now use the same hexagonal tile, so no separate arc-shaped tiles are needed. Ice and standard floor must look clearly different and use the friction values of Sections 6.1–6.2. The detailed art and audio lists are in Chapter 12; every asset is declared with its source and generator in Appendix C.

## 9.6 Level validation

Levels are validated in three steps: a simple blockout to check space and motion, tests with all six fighters, and a recheck after art, effects and sound are added. Single Player is tested with one human and five bots; Multiplayer with six humans, under both session rules. The criteria below are targets to verify, not results already achieved. Each session records the build version, layout, parameter set, level, seed, spawns, who controlled each fighter and the machine and network conditions.

| ID | Test situation | Criterion or data to record | Reference |
|---|---|---|---|
| LD01 | Start a match from the six spawns | No overlap, no fall or hazard before a warning; floor and nearby edge clearly visible | 9.1; T06/T09 in 14.3 |
| LD02 | A newcomer does the tutorial without extra explanation | Five steps completed; median at most 90 s | 3.7, 9.3, 14.2 |
| LD03 | Ride the reversals and meet every obstacle box and pusher block during full L2 cycles | No pinching, clipping or wrong elimination; no bot stuck over 3 s | 7.2; AI-6 in A.7 |
| LD04 | Watch the cannons from the six L3 spawns | Warnings on time and readable; record any hidden signal | 7.3; T09 in 14.3 |
| LD05 | Check every tile wave in L4 | At least 37 solid tiles and connectivity kept; effect of the whole group checked | 7.4; T10 in 14.3 |
| LD06 | Measure slides and bots leaving cracked tiles | Compare at the same weight class and speed; D3–D5 leave in at least 90% of valid cases | 6.2; AI-2 in A.7 |
| LD07 | Handle several warnings at once in L5 | A feasible route exists; warnings do not hide each other; record unclear falls | 7.5, 9.2.5 |
| LD08 | A ring drop while a fighter jumps, and the time limit while a tile respawns or a floor moves | A dropped ring never returns; the limit ranks survivors by Wobble on every PC | 7.1; T07/T08/T10/T20 in 14.3 |
| LD09 | Repeat matches rotating players through spawns | Win rate, survival time and first eliminations per spawn | 14.2 |
| LD10 | Play with sound off and in greyscale | Danger read through shape, icon and flashing; at least 4 of 5 players right first time | 11.4 |
| LD11 | Play one match on six PCs | Floor state, warnings and results consistent; a respawning tile never shown as solid | T06/T15 in 14.3 |
| LD12 | End matches in Single and Multiplayer under both rules | Points, lives, ranks, knockouts and transitions follow Sections 3.5–3.6 | 3.5–3.6; T05, T18 |
| LD13 | Rank respawns on every level, including right before an L1 drop | Respawn point on valid floor, at least 1.5 m inside the edge and outside active hazards; never on a band that drops within 3 s | 3.6, 7.1; T05, T20 |

LD05 tests the tile mechanic on its own, without fighters pushing the tile choice. Spawn fairness requires rotating players and recording the bot setup, seed and familiarity with the controls. The 30-match mark of Section 14.2

detects clear disadvantages but is not enough for a statistical balance claim. If a spawn is often eliminated first, review its distance to the edge, the hazard directions and escape routes before changing shared combat values.

Decisions settled in v0.5. The L2 shuttles no longer overlap the static floor (amplitude 1.5 m, rest centre ±9.5 m); a fire cannon with no target in range aims at a floor point 13 m along its sweep line (Section 7.3); L4 and L5 use hexagonal tiles with outer radius about 9 m and the 60% solid-tile rule; a match result is confirmed 0.5 s after the last elimination (Section 3.6). Decisions settled in v0.6. L1 becomes a 12 m arena island with four ring drops; L3 becomes a round 9 m floor with the cannons on the rim; the fighter's weight class is fixed by the chosen fighter. Decisions settled in v1.1. No arena shrink and no sweeper bar: L2–L5 end at a 150 s time limit and the ring drop becomes H01. Decisions settled in v1.2. L2 gains the counter-rotating obstacle ring (H07); L5 drops the turntable and becomes the L4 ice field with two cannons. Decisions settled in v1.3. The L2 side shuttles become two pusher blocks crossing the rings (H03). These decisions are logged in Appendix D.

Hand-over. Each arena needs a scaled map with legend, spawn points, hazard configuration, content list, test record and open issues. Minh Nhật maintains the layouts and their presentation; Đình Duy checks them against physics, AI and networking. A level is accepted only when the build provides the test evidence.
