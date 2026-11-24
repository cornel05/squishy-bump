# 2. Features and references

> **In this chapter:** The features players will notice first, and what we take from five existing games.

## 2.1 Player-facing features

| Feature | Why it matters in play | Design evidence |
|---|---|---|
| Fighter weight classes | Six fighters in three classes give clear match-ups: Heavy anchors, Light escapes, Normal balances. Picking a fighter is a commitment for the whole session. | Sections 6.4 and 4.2 (PF02) |
| Living Arena | The arena is a third player that is readable and fair: every hazard is telegraphed before it acts. | Section 7 (H01–H07) |
| Wobble knockback | Every hit makes the next one stronger, so matches build tension and a lucky first bump never decides everything. | Section 6.3 |
| Two session rules | Survival rewards staying alive; Rank rewards pushing others out, with respawns that keep everyone playing until time is up. | Sections 3.5 and 3.6 |
| Five-tier bots | Single-player always has a full lobby whose skill rises with the level. | Appendix A |
| Six-player online | Friends can play on one LAN or over the Internet with a join code. | Section 10.4 |

## 2.2 Reference games and lessons

| Reference | Relevant lesson | Application and boundary |
|---|---|---|
| Sumo (Table 9 Studio, Steam) | Spherical fighters, abilities and collapsing arenas make a readable ring-out game for four players. | We add fixed weight classes per fighter and a level progression; no per-character abilities. |
| Fall Guys, Stumble Guys | Short rounds, cute characters and physical comedy appeal to a wide audience. | We keep one arena per match and six players, not mass elimination of dozens. |
| Gang Beasts | Throwing rivals into hazards is funnier than reducing a health bar. | We use rigid spheres with a jelly squash visual instead of ragdolls to keep physics and networking tractable. |
| Super Smash Bros. | Knockback that grows with damage creates comebacks and climaxes. | Our Wobble meter counts hits, decays over time and is capped. |
| Rocket League | Online physics needs a fixed tick and server authority. | We use host authority with local movement prediction, not full physics rollback. |
