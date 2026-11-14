# 3. How to play

> **In this chapter:** A match from the player's seat: loop, controls, modes, rules and onboarding.

## 3.1 Playing a match

You are the Apple, a Normal-class fruit, and you land on the Cannon Court with five other jelly fruits. The countdown ends and everyone rushes to the centre. You dash into the Peach, a Light fruit, and it slides almost five metres toward the edge. Then a cannon on the rim glows and a cone appears on the floor. The Watermelon beside you, a Heavy fruit, barely moves in the gust, but you step out of the cone just in time while the Peach flies off the court.

Your Wobble meter shows four hits, so each new bump throws you further. With ten seconds left the clock turns red. Two fruits are left: you and the Watermelon. You cannot out-push a Heavy fruit, so you wait for it to charge, sidestep, and it rolls past you into the void. You are the last one standing. Had the clock run out first, the fruit with the lower Wobble would have won. Under the Rank rule the same push would also have scored you a point, and a fallen fruit respawns until its three lives are gone or time is up.

## 3.2 Play outline

| Template prompt | Player experience and governing section |
|---|---|
| Opening the game application | Main menu: Single Player, Multiplayer, Settings, Credits, Quit; then the rule choice and the walkable 3D lobby (Sections 5.3 and 11). |
| Game options | Key rebinding, mouse sensitivity, camera distance, volumes, language, screen shake on or off. |
| Story synopsis | After closing time, six squishy jelly fruits in a toy store come alive and compete across five play areas of the store for the Golden Squish trophy on the top shelf (Section 8). Opening and ending cutscenes frame single-player runs. |
| Modes | Single Player or Multiplayer, each with the Survival or Rank session rule (Section 3.5). |
| Game elements | Move, jump, dash, bump, fruit weight classes, Wobble knockback, lives and respawn (Rank), living-arena hazards (Sections 3.3, 6, 7). |
| Game levels | Five arenas played in order, each adding one mechanic: Training Ring, Moving Ground, Cannon Court, Frost Ring, Chaos Core (Sections 9 and 7). |
| Player controls | Keyboard and mouse (Section 3.4). |
| Winning | Survival: be the last fruit on the floor. Rank: score the most ring-out points over the session (Section 3.6). |
| Losing | Survival: falling off eliminates the fruit for that match. Rank: each fall costs one of three lives; with no lives left the fruit is out of the match (Section 3.6). |
| End | Session results and podium, then an ending cutscene in single-player and a return to the menu. |
| Why is all this fun | Instant readability, squishy physical comedy, comeback moments from the Wobble rule and class match-ups against a moving arena (Section 2.1). |

## 3.3 Core loop

Read the arena, take a position that suits your weight class, then bump, dash or dodge. Successful bumps raise the opponent's Wobble meter, so the next hit throws them further, until a ring-out removes them. In Survival the last fruit standing wins the match; in Rank every ring-out scores a point for the last fruit that touched the victim, and the victim respawns while it has lives left. A fruit that is out spectates (or retries the level in Survival single-player) and returns to the loop in the next match.

*Figure 1: Core loop*

## 3.4 Player controls, actions and feedback

| Input | Action | Feedback and limit |
|---|---|---|
| WASD | Move (camera-relative) | The fruit rolls with a soft squelch; acceleration depends on weight class and floor (Section 6.2). |
| Mouse | Orbit camera | Sensitivity adjustable; pitch clamped between 15 and 60 degrees. |
| Space | Jump | Only when grounded; 1.2 m apex; squash on take-off and landing puff. |
| Shift or left mouse | Dash | Impulse in the move direction (or facing if idle); trail and whoosh; 3.0 s cooldown ring on the HUD. |
| Tab | Scoreboard | Hold to view placements and points; the game keeps running. |
| Esc | Menu | Pauses only in single-player. In multiplayer it opens an overlay while the match continues. |

Player feedback for a bump: hit-stop of 40 ms on the host-confirmed hit, jelly squash and stretch on both fruits, a squish sound whose pitch rises with impulse, a screen shake proportional to the received impulse, and a Wobble counter pop on the victim. A fighter can never attack directly: every effect comes from movement, mass and the arena. The weight class is fixed, so the controls stay at six inputs.

## 3.5 Modes and session structure

Terminology follows Section 5.2: a level is one arena; a match is one play of a level; a run or session is the ordered sequence of levels, always starting at L1. Single Player always plays L1 to L5; in Multiplayer the host chooses the length L1 to Lk (k = 1 to 5).

| Rule | Survival · Single | Survival · Multiplayer | Rank · Single or Multiplayer |
|---|---|---|---|
| Players | 1 human + 5 bots | 2–6 humans | 1 human + 5 bots, or 2–6 humans |
| Level order | Always L1 to L5 | L1 to Lk, k chosen by the host (1–5) | Single: L1 to L5; Multiplayer: L1 to Lk |
| In a match | One life; advance only as the sole last fruit, otherwise retry the same level | One life; after the match the lowest-placed players are knocked out of the session (formula below) | Three lives; respawn 2 s after a fall until the time limit (L1: until the third ring drop); always continue to the next level |
| Points | Not used | Not used; final rank follows knockout order | +1 per ring-out for the last toucher within 3 s; points accumulate over the session |
| Session end | After clearing Lk: victory cutscene | When one champion remains: podium | After the last level: final ranking, podium (ending cutscene in Single) |

```text
n_out = ⌈ (N − 1) / L_left ⌉
```

Multiplayer Survival knockout. After each match, the n_out lowest-placed of the N players still in the session are knocked out, where L_left counts the levels left including the one just played. The session therefore always ends with exactly one champion on the last level: six players over five levels lose one player per match, and six players over two levels lose three, then two. Knocked-out players spectate the rest of the session; the final ranking follows knockout order.

Bots exist only in single-player and take the five fruits the player did not choose, so every class is always on the floor. In Survival the bot roster and the hazard seed are kept for retries, so a retry tests the player's skill rather than luck. Retries are unlimited; the run summary shows how many were used.

## 3.6 Winning, losing and edge cases

- Ring-out. A fruit falls out when its centre drops below the kill plane (y = −5 m) or leaves the arena bounds. In Survival this eliminates it from the match. In Rank it loses one life and, if lives remain and respawns are still open (until the time limit; on L1 until the third ring drop at 105 s), it respawns after 2 s with 1.5 s of invulnerability at a safe point; its Wobble resets to 0.
- Elimination and placement. A fruit with no lives left (one life in Survival) is out of the match. Elimination order decides placement: the first fruit out takes the last rank. The match ends when one fruit remains, which takes rank 1, or when the time limit is reached.
- Rank points. Each ring-out gives +1 point to the last other fruit that touched the victim within the previous 3 s (bump, dash or being pushed into it). A fall with no such toucher, including a hazard-only fall, gives no point. Respawn points are chosen on the remaining floor, at least 1.5 m inside the current edge and as far as possible from other fruits and active hazards.
- Same-tick eliminations share the better rank and the following rank is skipped. If the last fighters fall in the same tick, the match is a draw: they share rank 1 and, in Survival, the player must retry because they were not the sole survivor.
- Session ranking. Survival single-player reports the levels cleared and the retries used; Survival multiplayer ranks by knockout order. Rank uses total points; ties are broken by total lives remaining, then total survival time.
- Result confirmation. The host confirms a match result 0.5 s after the last elimination it detects, so falls that happen almost together are ranked identically on every PC.
- Time limit. Every match on L2–L5 ends at 150 s; on L1 the last ring drops at 135 s, so the floor is gone first (Section 7.1). If two or more fruits are still alive at the limit, they are ranked by Wobble, lowest first; equal Wobble shares the rank. In Survival single-player the player advances only as the sole first-ranked fruit. Under Rank the points already scored stand, and placement follows lives left, then Wobble.
- No soft-lock. Because of the ring drops on L1 and the time limit on L2–L5, every match ends by 150 s even if every fruit stands still.

## 3.7 Onboarding

Level 1 (Training Ring) is the tutorial on the first single-player run. Context prompts appear one at a time and advance when the action is performed: move, jump, dash, then push a stationary training toy off the mat. A short card explains the player's weight class with one sentence and one icon. Only then do the five bots activate, all at difficulty D1, and the first ring drop at 45 s teaches players to watch the floor. The prompts can be skipped and are not shown again once completed. In multiplayer, the 3D lobby has a controls board that players can walk up to.
