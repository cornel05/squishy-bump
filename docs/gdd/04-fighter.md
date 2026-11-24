# 4. The fighter

> **In this chapter:** Every state a fighter has, how it changes and how the player sees it.

## 4.1 Fighter definition

Every player controls one squishy animal. All fighters share the same rules; the only statistical difference is the weight class, which comes from the fighter chosen in the lobby and stays fixed for the session. Colour and fighter silhouette identify the player. There is no health and no weapon. A fighter's only actions are moving, jumping and dashing, and its only way to be defeated is a ring-out (or, under Rank, running out of lives).

## 4.2 Fighter properties

| Property ID | State and changing action | Effect and player feedback |
|---|---|---|
| PF01 Movement | Horizontal velocity under the traction rule; v_max by weight class | Rolling visual and squelch pitch follow speed |
| PF02 Weight class | Heavy, Normal or Light; set by the fighter picked in the lobby; never changes during the session | Mass, radius, top speed and traction; silhouette size; class badge on the HUD and in the lobby |
| PF03 Dash | Ready or cooling (3.0 s); dash state 0.35 s after use | Δv = 80 / m; impulse bonus 1.25 while dashing; trail and whoosh |
| PF04 Jump | Grounded or airborne | Apex 1.2 m; 30% air control; landing puff |
| PF05 Wobble h | 0 to 10; +1 per hit, +2 per fire blast; −1 per 6 s without a hit; reset each match and on respawn | Knockback K = 1 + 0.12h (max 2.2); counter above the fighter; face looks dizzier as h rises |
| PF06 Stun | 0.15 + 0.02h s after a hit | Input ignored (buffered for 0.1 s); stars over the head |
| PF07 Activity and lives | Alive, Respawning (Rank, 2 s, then 1.5 s invulnerable), Eliminated (spectating) or Disconnected; lives 3 → 0 under Rank | Fall splash, lives hearts on the HUD, blinking outline while invulnerable; spectator camera cycles alive fighters |
| PF08 Placement and points | Rank per match by elimination order; under Rank, +1 point per credited ring-out; session total | +1 pop over the credited fighter, result screen, scoreboard (Tab), podium |

## 4.3 Rewards

| Reward | Acquisition and feedback |
|---|---|
| Match win | Last fighter standing; crown pop, slow-motion final ring-out, victory jingle. |
| Ring-out points (Rank) | Every credited ring-out; +1 pop above the pusher and animated score bars at the result. |
| Level clear (Survival) | Advance to the next arena; retry count shown. |
| Session podium and ending | Top three on the podium; single-player ending cutscene. |
| Personal records | Best Survival run and best Rank score saved locally. |

There are no pickups, power-ups, currency or unlockable statistics in the minimum product. Rewards are status and feedback only, so every match starts equal.

## 4.4 Action priorities

Within one tick the host processes a fighter's input in this order: jump, dash, then movement. A dash is allowed in the air once per airtime and is rejected during stun or respawn. Inputs arriving during stun are buffered for at most 0.1 s; inputs during the 2 s respawn wait are discarded. The host validates every cooldown, so a client cannot dash faster by sending more commands.
