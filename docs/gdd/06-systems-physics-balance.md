# 6. Systems, physics and balance

> **In this chapter:** All numbers and formulas that run the game, with worked examples.

## 6.1 Design quantities and initial balance values

These starting values are stored in the BalanceProfile and ClassProfile assets and are tuned through playtests. Every change is recorded in the change log with its reason.

| System | Quantity | Initial value | Purpose |
|---|---|---|---|
| Simulation | Fixed physics step | 60 Hz (Δt = 1/60 s) | Stable, reproducible collisions |
| Simulation | Game gravity g | 20 m/s² | Snappy jumps (about 2g) |
| Weight class | Mass m (Heavy / Normal / Light) | 15 / 10 / 7 kg | Core trade-off of the fighter weight classes |
| Weight class | Radius r | 0.60 / 0.50 / 0.45 m | Readable silhouette |
| Weight class | Max speed v_max | 4.5 / 6.0 / 7.5 m/s | Light escapes, Heavy anchors |
| Weight class | Traction factor c | 0.8 / 1.0 / 1.2 | Acceleration differences |
| Weight class | Fighters per class | 2 / 2 / 2 (Bear, Pig / Cat, Penguin / Chick, Frog) | Every class is on the floor in single-player |
| Presentation | Per-map visual fighter scale | L1 0.95× · L2 0.80× · L3 0.60× · L4 0.90× · L5 0.85× | Balances character readability against each Creative map (visual-only; physics sphere radius r 0.60/0.50/0.45 m unchanged) |
| Movement | Floor friction μ | 0.6 | a_max = c·μ·g = 9.6 / 12 / 14.4 m/s² |
| Movement | Ice friction μ_ice (Frost Ring, Chaos Core tiles) | 0.3 | a_max = 4.8 / 6.0 / 7.2 m/s²; slides twice as far |
| Movement | Air control | 30% of a_max | Limited mid-air correction |
| Jump | Apex height / launch speed | 1.2 m / 6.93 m/s | Air time 0.69 s; dodge low hits |
| Dash | Impulse J_d / cooldown | 80 N·s / 3.0 s | Δv = 5.3 / 8.0 / 11.4 m/s |
| Dash | Dash state / bonus b | 0.35 s / 1.25 | Rewards committing to a charge |
| Collision | Restitution e (fighter-fighter) | 0.6 | Bouncy but controllable |
| Collision | Hit threshold | 30 N·s | Ignore light touches |
| Wobble | Knockback K(h) / cap | 1 + 0.12h, max 2.2 (h ≤ 10) | Rising tension, bounded |
| Wobble | Decay | −1 per 6 s without a hit | Comeback for careful players |
| Stun | Input lock after a hit | 0.15 + 0.02h s | Readable reaction |
| Match | Time limit (L2–L5) / tie rule at the limit | 150 s / lowest Wobble ranks first | No stalls; rewards careful play |
| Match | Ring drops on L1 (3 m per ring) | 45 / 75 / 105 / 135 s; 3 s warning | End-phase pressure on L1, readable in steps |
| Match | Kill plane | y = −5 m | Unambiguous elimination |
| Rank rule | Lives / respawn delay / invulnerability | 3 / 2.0 s / 1.5 s | Everyone keeps playing until time is up (L1: until the third drop) |
| Rank rule | Last-touch window / points per ring-out | 3.0 s / +1 | Credits the pusher, not the hazard |
| Network | Input / snapshot rate / interpolation buffer | 60 Hz / 30 Hz / 100 ms | Section 10.4 |

## 6.2 Movement and traction

A fighter is a PhysX rigidbody with a sphere collider against the static arena, but its horizontal motion is controlled by the team's PlayerMotor. Each tick it moves the horizontal velocity toward a target velocity, limited by the traction the floor can provide:

```text
v_target = v_surface + v_max · u        a_max = c · μ · g
```

```text
v ← v + clampLength(v_target − v, a_max · Δt)
```

u is the camera-relative input vector (length at most 1) and v_surface is the velocity of the floor under the fighter (zero on static floors, non-zero on the turntable and the obstacle ring; Section 7). The same rule brakes the fighter when there is no input and slows it after a hit, so a launched fighter slides a distance s = Δv² / (2·a_max). On the standard floor a Normal fighter reaches full speed in 6.0 / 12 = 0.5 s; Heavy needs 0.47 s and Light 0.52 s, so the classes feel equally responsive while their top speeds differ.

The jump sets the vertical velocity to √(2·g·h) = √(2 × 20 × 1.2) = 6.93 m/s, giving an air time of 2 × 6.93 / 20 = 0.69 s. The dash adds Δv = J_d / m along the dash direction; speed above v_max then decays through the traction rule.

Ice. On ice tiles μ drops from 0.6 to 0.3, so a_max halves. Reaching full speed takes 0.94 / 1.0 / 1.04 s (Heavy / Normal / Light) instead of about 0.5 s, braking takes twice as long, and every slide distance doubles. No extra code is needed: the same traction rule reads the friction of the floor under the fighter.

The character visual (RollVisual) is a child of the spherical physics body. Replacing the earlier rolling-sphere prototype (D39), the six animal fighters stay upright and rotate smoothly to face their horizontal movement direction instead of rolling head-over-heels, while the parent collider and camera never roll. A jelly squash-and-stretch (scale 1.15 × 0.85 for 0.12 s) plays on take-offs, landings and hits. In addition to weight-class sizing (r = 0.60 / 0.50 / 0.45 m), the visual model applies a per-map visual scale (L1 0.95×, L2 0.80×, L3 0.60×, L4 0.90×, L5 0.85×) to fit each Creative arena's proportions; both squash and per-map scale are visual only and never alter the physics collider.

## 6.3 Collisions and Wobble knockback

Fighter-to-fighter contacts are not left to PhysX: fighters are on a layer that ignores other fighters, and the CollisionResolver tests the 15 possible pairs of six fighters each tick. For a pair A, B with centres p_A, p_B, contact exists when |p_B − p_A| < r_A + r_B. With the unit normal n = (p_B − p_A) / |p_B − p_A| and relative velocity v_rel = v_B − v_A, the fighters are approaching when v_rel · n < 0, and the impulse is:

```text
j = −(1 + e) (v_rel · n) / (1/m_A + 1/m_B)
```

```text
v_A ← v_A − (j_A / m_A) n        v_B ← v_B + (j_B / m_B) n        j_i = j · K(h_i) · b_i
```

K(h_i) = min(1 + 0.12·h_i, 2.2) is the Wobble multiplier of the receiver, and b_i = 1.25 when the other fighter is in its dash state (otherwise 1). The multiplier intentionally breaks momentum conservation: it is a game rule that makes damaged fighters easier to ring out. Overlap is then removed by moving each fighter along n in proportion to its inverse mass, correcting 80% of the penetration beyond a 0.01 m slop to avoid jitter. A received j_i of at least 30 N·s counts as a hit: h increases by 1 and the receiver is stunned for 0.15 + 0.02·h seconds.

Worked example: a dashing Normal fighter closes at 10 m/s on a stationary target on the standard floor. The slide distance uses the target's own braking a_max.

| Target class | j (N·s) | j × b (N·s) | Δv at h = 0 | Slide at h = 0 | Δv at h = 5 | Slide at h = 5 |
|---|---|---|---|---|---|---|
| Heavy | 96.0 | 120.0 | 8.0 m/s | 3.3 m | 12.8 m/s | 8.5 m |
| Normal | 80.0 | 100.0 | 10.0 m/s | 4.2 m | 16.0 m/s | 10.7 m |
| Light | 65.9 | 82.4 | 11.8 m/s | 4.8 m | 18.8 m/s | 12.3 m |

The Training Ring starts with a 12 m radius, so a single bump from the centre never rings out a fresh fighter (at most 4.8 m). After five hits a Light fighter can already be pushed out from the centre in one dash (12.3 m), and once the first ring drops at 45 s (radius 9 m) a Normal fighter can too (10.7 m). The attacker also recoils by j / m_A: 9.6, 8.0 and 6.6 m/s against Heavy, Normal and Light, so charging a Heavy fighter mostly stops the attacker.

## 6.4 Weight-class trade-offs

| Property | Heavy (Bear, Pig) | Normal (Cat, Penguin) | Light (Chick, Frog) |
|---|---|---|---|
| Best use | Holding the edge, taking a cannon gust, staying on the turntable, blocking a charge | General fighting; flexible on every level | Escaping dropping rings, tiles, blasts and pusher lanes, chasing |
| Weakness | Slow; dash gives only 5.3 m/s | No clear edge in any situation | Flies far when hit |
| Dash Δv | 5.3 m/s | 8.0 m/s | 11.4 m/s |
| Air gust at 2 m (72 N·s) | 4.8 m/s | 7.2 m/s | 10.3 m/s |
| Slide on ice after the dash hit in Section 6.3 (h = 0) | 6.7 m | 8.3 m | 9.6 m (out from the centre of a 9 m ring) |
| Visual cue | Largest silhouette (r 0.60 m), deep squelch | Medium silhouette (r 0.50 m), mid squelch | Smallest silhouette (r 0.45 m), high squeak |

On ice the weight class becomes decisive: a fresh Light fighter can be rung out from the centre of Frost Ring by a single dash, while a Heavy fighter keeps more than 2 m of margin. This is intended and is the identity of Level 4; Light fighters answer it with speed and by avoiding the centre.

The class is chosen once in the lobby and kept for the whole session, so the trade-off is a commitment rather than a reaction. Each class has a level where it shines: Heavy on Moving Ground and Cannon Court, Light on the Training Ring drops and among the falling tiles, Normal everywhere. Two fighters share each class, so up to two players can pick their preferred class in a six-player lobby.

## 6.5 Hazard forces and ballistics

Air gust (H04). Inside a cone of half-angle 15° and range L = 10 m, a fighter at distance d receives a continuous force for t_g = 0.6 s, scaled by its Wobble multiplier:

```text
F(d) = F₀ · (1 − d / L) · K(h)        F₀ = 150 N        J = F · t_g
```

Fire blast (H05). The fireball is launched at θ = 45° toward the target point at horizontal distance d and height difference Δh. The launch speed and flight time are:

```text
v₀ = √( g·d² / (2·cos²θ · (d·tanθ − Δh)) )        t = d / (v₀ · cosθ)
```

On a flat arena (Δh = 0) this reduces to v₀ = √(g·d); for d = 12 m, v₀ = 15.5 m/s and t = 1.10 s. The fireball is then integrated by the same fixed-step gravity. On impact it applies a radial impulse to every fighter within R_b = 3 m, directed outward with a 30% upward component, and adds 2 to Wobble:

```text
J(d) = J₀ · (1 − d / R_b) · K(h)        J₀ = 90 N·s
```

Surface motion (H02, H07). On the turntable, v_surface = ω × r_c, where r_c is the fighter's offset from the arena centre; the outer ring uses its own ω_o, opposite to the turntable. The traction rule of Section 6.2 carries fighters with the floor; no fictitious centrifugal force is simulated.

Obstacle boxes (H07) and pusher blocks (H03). Each H07 box is a kinematic collider fixed to the outer ring at radius 5.9 m and moving at |ω_o|·5.9 ≈ 1.5 m/s; each H03 block is a kinematic collider moving along a straight lane at 3.0 m/s. A fighter that touches either receives one impulse along the obstacle's direction of travel t̂ plus, for H07, an outward component along the radial direction r̂, scaled like any hit; repeated contact within 0.5 s is ignored. Both are 0.8 m tall, so a jump (apex 1.2 m) clears them:

```text
J_b = 50 N·s · K(h) · normalize(t̂ + 0.5 r̂)  (H03: r̂ term = 0)        Δv = 3.3 / 5.0 / 7.1 m/s (h = 0)        slide = 0.6 / 1.0 / 1.8 m
```

## 6.6 Progression, scoring and difficulty curve

Progression changes the arena and the opponents, never the player's statistics. Difficulty rises along two axes: one new hazard type per level, and bot skill (Appendix A). The bot roster for level N always contains one bot at tier D_N; the other four are drawn uniformly from tiers max(1, N − 2) to N.

| Level | Arena | New mechanic | Bot tiers | Mean tier | Target median match |
|---|---|---|---|---|---|
| L1 | Training Ring | Movement, bumping and ring drops (tutorial) | D1 | 1.0 | 60 s |
| L2 | Moving Ground | Reversing turntable, counter-rotating obstacle ring and crossing pusher blocks | D1–D2 | 1.6 | 70 s |
| L3 | Cannon Court | Air and fire cannons around the rim | D1–D3 | 2.2 | 75 s |
| L4 | Frost Ring | Ice floor with cracking tiles | D2–D4 | 3.2 | 75 s |
| L5 | Chaos Core | Ice tiles of L4 with the cannons of L3 | D3–D5 | 4.2 | 85 s |

Mean tier = (N + 4 × mean of the drawn range) / 5; for example L4 gives (4 + 4 × 3) / 5 = 3.2. There is no arena shrink: L1 ends with its ring drops (H01) and L2–L5 end at the 150 s time limit, so pressure on L2–L5 comes from each level's own hazards and from the bots.

*Figure 3: Difficulty curve: bot tier by level*

Economy. There is no currency and no pickup in the minimum product. The only resources are the dash cooldown (3.0 s), the opponent's Wobble, which the attacker builds and the defender lets decay, and, under Rank, the three lives per match. Rank points are the session currency: one point per credited ring-out, so a fighter that plays aggressively near the edge earns more but also risks more lives.

## 6.7 AI module summary

Bots use a finite-state machine (Countdown, Roam, Chase, Charge, Evade, Recover, Eliminated), utility scoring to choose a target, a danger map built from hazard telegraphs, and A\* pathfinding on a 1 m grid whose cost rises with danger. Five profiles D1 to D5 change reaction delay, aim noise, prediction, edge margin, hazard awareness and how well the bot plays to its own and its target's weight class. Bots send the same input commands as human players to the same PlayerMotor, so they obey identical physics. Appendix A gives the full contract.
