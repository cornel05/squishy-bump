# Appendix A. AI behaviour specification

> **In this chapter:** The complete contract for the bots: states, scoring, pathfinding and five tiers.

## A.1 Architecture

Each bot runs a sense-think-act cycle on the host at its decision rate (4 to 10 Hz by tier). BotSensors read the world state and the hazard events; BotBrain updates the danger map, chooses a target, runs the state machine and plans a path; the result is an input command (move vector, jump, dash) fed to the same PlayerMotor as a human player. Each bot plays the fruit it received in the lobby and knows its own weight class. Reaction delay is modelled by acting on sensor data that is older by the tier's delay.

## A.2 State machine

| State | Behaviour | Exit conditions |
|---|---|---|
| Countdown | No input | GO → Roam |
| Roam | Move toward the lowest-danger cell near the centre | Target chosen (U ≥ 0.4) → Chase; own danger ≥ 0.5 → Evade |
| Chase | Follow an A\* path to the approach point behind the target, on the side away from the target's nearest edge | Distance ≤ 3.5 m and aim error ≤ tier alignment → Charge; danger ≥ 0.5 → Evade; target lost → Roam |
| Charge | Dash toward the predicted target position p + v·lead, with aim noise | After the dash state → Recover |
| Evade | Move to the cheapest safe cell within 4 m; jump an obstacle box or a pusher block if the tier allows | Danger < 0.2 → Roam |
| Recover | Brake for 0.4 s and face the arena centre | Timer → Roam; danger ≥ 0.5 → Evade |
| Eliminated | No input; under Rank the bot waits 2 s and re-enters Roam at its respawn point while lives remain | Match end |

Anti-stuck rule: if a bot makes less than 0.5 m of progress toward its goal in 2 s, it picks a random safe cell and returns to Roam.

## A.3 Utility target selection

```text
U(t) = 0.4·(1 − min(d/12, 1)) + 0.3·(1 − min(e_t/6, 1)) + 0.2·(h_t/10) + 0.1·s_t − 0.5·D_path
```

d is the distance to the target, e_t the target's distance to the nearest edge, h_t its Wobble, s_t its class vulnerability relative to the bot (1 when the target is lighter, 0.5 when equal, 0 when heavier) and D_path the mean danger along the planned path. A bot changes target only when another fruit scores at least 0.15 higher (hysteresis). Bots do not prefer the human player, so Rank results stay fair. D1 bots ignore utility and pick a random target every 4 s.

## A.4 Danger map and pathfinding

The arena is covered by a grid of 1 m cells (at most 26 × 26). Each cell gets a danger value D(c) between 0 and 1: the maximum of an edge term 1 − e(c) / margin (clamped at 0) and a term 1 − τ / 2 s for every telegraphed hazard whose area contains the cell, where τ is the time until it acts. Fallen tiles and cells outside the arena are blocked. A\* searches eight neighbours with cost 1 + 8·D(c) and a Euclidean heuristic, and replans at each decision. Which hazards a bot includes depends on its tier. On ice cells, tiers D3 and above multiply their edge margin by 1.5 because braking distance doubles.

## A.5 Difficulty ladder

| Parameter | D1 | D2 | D3 | D4 | D5 |
|---|---|---|---|---|---|
| Decision rate | 4 Hz | 5 Hz | 6 Hz | 8 Hz | 10 Hz |
| Reaction delay | 0.50 s | 0.40 s | 0.30 s | 0.20 s | 0.12 s |
| Aim noise σ | 25° | 18° | 12° | 7° | 3° |
| Charge alignment | 35° | 30° | 25° | 20° | 15° |
| Target lead | 0 s | 0 s | 0.2 s | 0.35 s | 0.5 s |
| Edge margin | 0.5 m | 1.0 m | 1.5 m | 2.0 m | 2.5 m |
| Speed factor | 0.85 | 0.90 | 0.95 | 1.00 | 1.00 |
| Targeting | Random | Utility | Utility | Utility | Utility |
| Hazard awareness | Edge and ring drops | + cannons, turntable reversal, obstacle boxes, pusher lanes | + tiles | + blasts, burn, time left | All, with time-to-impact |
| Ice handling | None | None | Edge margin ×1.5 on ice | Margin ×1.5; brakes early | Margin ×1.5; plans slides with μ_ice |
| Class-aware tactics | None | None | Avoids charging heavier fruits head-on | Heavy holds the centre; Light keeps distance and flanks | Predictive: baits heavier fruits into hazards, punishes lighter ones near edges, plays for low Wobble near the time limit |

## A.6 Roster rule

For level N the host creates five bots: one at tier D_N and four drawn uniformly from tiers max(1, N − 2) to N using the match seed. The bots take the five fruits the player did not pick, in seeded order, and keep them for the whole run. Survival retries keep the same roster, fruits and seed, so the same roster is recognisable across retries.

## A.7 AI test scenarios

| ID | Scenario | Pass condition |
|---|---|---|
| AI-1 | One bot alone on the Training Ring for 120 s, each tier | D2–D5 never fall before the third ring drop; D1 falls in at most 1 of 5 runs |
| AI-2 | Bots on Frost Ring for 60 s | D3–D5 leave a warned tile before it falls in at least 90% of cases |
| AI-3 | D5 against D1, 1v1, 20 matches | D5 wins at least 80% |
| AI-4 | Adjacent tiers, 1v1, 20 matches each | Higher tier wins 55–75% (tiers are distinct but not crushing) |
| AI-5 | Five D5 bots on Chaos Core, profiler on | All bot logic under 1 ms per frame on average |
| AI-6 | Bots on Moving Ground for 120 s | No bot stuck more than 3 s; anti-stuck rule triggers correctly; D4–D5 avoid or jump at least 70% of obstacle boxes and pusher blocks |
