# 7. Living Arena hazards

> **In this chapter:** Seven autonomous hazards: parameters, telegraphs, physics and counter-play.

Every hazard is simulated by the host, scheduled from the match seed and announced to clients by a reliable event carrying the host tick at which it acts. Each hazard has a telegraph of at least 1.0 s. Hazards affect bots and humans identically.

| ID | Hazard | Levels | Summary |
|---|---|---|---|
| H01 | Ring drop | L1 | The outermost 3 m ring drops at 45, 75, 105 and 135 s |
| H02 | Turntable | L2 | Central disc rotates and reverses |
| H03 | Pusher blocks | L2 | Two blocks run back and forth across the arena and push fruits off |
| H04 | Air cannon | L3, L5 | Sweeping cone gust |
| H05 | Fire cannon | L3, L5 | Lobbed fireball, blast, burning patch |
| H06 | Cracking ice tiles | L4, L5 | Slippery ice tiles crack, fall and respawn |
| H07 | Obstacle ring | L2 | Outer ring turns against the turntable; its boxes push fruits off |

## 7.1 H01 Ring drop (Training Ring)

The Training Ring floor is a play mat of radius 12 m painted as four concentric bands, each 3 m wide. At 45, 75, 105 and 135 s the outermost remaining band drops away, so the radius goes 12 → 9 → 6 → 3 → 0 m. Each drop is announced 3 s ahead: the band's inner seam flashes amber, then red at 2 Hz in the last 1.0 s, with a rising creak. A fruit still on the band when it drops falls with it. Because the third drop (105 s) leaves only a 3 m disc, respawns under Rank stop from that moment. The fourth drop removes the last disc, so the match ends by about 136 s. The other levels have no shrinking floor; they end at the 150 s time limit (Section 3.6).

## 7.2 H02 Turntable, H07 obstacle ring and H03 pusher blocks (Moving Ground)

| Parameter | H02 Turntable (centre ring) | H07 Obstacle ring | H03 Pusher blocks |
|---|---|---|---|
| Geometry | Disc radius 4 m at the arena centre | Ring from radius 4 to 6.5 m around the turntable; three boxes 1.2 (along the ring) × 1.0 × 0.8 m at radius 5.9 m, 120° apart; the inner lane (4–5.4 m) has no boxes | Two blocks 1.5 (along the lane) × 1.0 × 0.8 m on two straight east–west lanes at z = +1.2 and −1.2 m, from a dock at x = −9.5 m to a dock at x = +9.5 m, crossing both rings; docks are off the floor |
| Motion | ω = 20°/s (0.35 rad/s); edge speed 1.4 m/s | ω_o = 15°/s opposite to H02; boxes at 1.5 m/s, outer edge 1.7 m/s | 3.0 m/s with a 0.3 s ease at each end; one pass ≈ 6.3 s; block A starts west, block B east, so they run in opposite directions |
| Change | Reverses every 15 s with a 2 s linear ramp | Reverses together with H02, so the two rings always turn in opposite directions | Waits at least 2 s at a dock, then departs only if the host predicts that no H07 box will cross its lane during the pass; first departure at 5 s |
| Telegraph | Arrow decals turn amber 2 s before reversal; ramp sound | Boxes always visible with an amber stripe on the leading face; whoosh within 2 m, that is 1.3 s ahead | Lanes painted with amber stripes at all times; dock light turns amber 1.0 s before departure, with a toy horn |
| Physics | Kinematic body; v_surface = ω × r_c | Kinematic ring, v_surface = ω_o × r_c; box impulse J_b (Section 6.5), +1 Wobble; jump to clear | Kinematic box; impulse J_b along the lane (Section 6.5), +1 Wobble; jump to clear |

## 7.3 H04 Air cannon and H05 fire cannon (Cannon Court)

Cannon Court is a round floor of radius 9 m with four cannons standing just outside the rim at radius 9.9 m: fire cannons at 45° and 225°, air cannons at 135° and 315°. Each cannon sweeps ±60° around its home direction (toward the centre) at 30°/s and fires every 5.0 s; the four cannons are staggered by 1.25 s so that no two fire at the same moment. In Chaos Core one air and one fire cannon stand on opposite sides (west and east) and fire every 4.0 s. Players never control cannons.

| Parameter | H04 Air cannon | H05 Fire cannon |
|---|---|---|
| Aim | Freezes its sweep yaw when the telegraph starts | Turns to the fruit closest to its sweep line at 10 to 16 m (seeded tie-break) and locks that fruit's position; with no fruit in range it targets a floor point 13 m along its sweep line, still telegraphed |
| Telegraph | 1.0 s barrel glow and a translucent cone on the floor | 1.0 s glow, then a landing marker during the 1.0–1.3 s flight |
| Effect | Cone 15° half-angle, 10 m range, 0.6 s gust, F₀ = 150 N; +1 Wobble if J ≥ 30 N·s | Blast R_b = 3 m, J₀ = 90 N·s, +2 Wobble; burning patch 2 m radius for 3 s adds +1 Wobble per full second inside |
| Counter-play | Step out of the cone; a Heavy fruit slides least | Leave the marker; Light fruits escape fastest |

## 7.4 H06 Cracking ice tiles (Frost Ring)

Frost Ring is a frozen lake: a circular floor of 61 hexagonal ice tiles, 2 m flat-to-flat (a centre tile plus four rings: 1 + 6 + 12 + 18 + 24, outer radius about 9 m), with friction μ_ice = 0.3, so every fruit slides about twice as far as on the standard floor (Section 6.2). Ice and cracking form one idea, a frozen floor that breaks, so the level still introduces a single new concept. Every 3.0 s the host selects k tiles (k = 2 for 0–30 s, 3 for 30–60 s, 4 after 60 s). A selected tile shows spreading white cracks, blinks red and shakes for 1.5 s, then shatters and falls. It respawns after 10 s with a 1 s fade-in; its collider turns on only when no fruit overlaps it.

- Weighted choice. Selection weight w = 1 + 1.5 × (number of fighters standing on the tile), so standing still is risky for everyone equally.
- Floor floor. At least 60% of tiles (37) stay solid at all times.
- Connectivity. A tile is skipped if removing it would split the largest solid region; this is checked by a breadth-first search over the 6-neighbour hexagonal tile graph.

## 7.5 Chaos Core combination

Chaos Core is the finale of the toy store: the full Frost Ring field of 61 H06 ice tiles (outer radius about 9 m, friction 0.3) with one H04 air cannon in the west and one H05 fire cannon in the east, standing just outside the rim at x = ±9.9 m. The tiles use a faster 3.5 s wave interval (k = 2, rising to 3 after 45 s) and the cannons fire every 4.0 s, so players must read cracks and cones at the same time. There is no turntable on this level; the same 60% solid-tile rule as L4 (at least 37 tiles) always applies.
