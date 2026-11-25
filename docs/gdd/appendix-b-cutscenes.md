# Appendix B. Cutscene script and triggers

> **In this chapter:** Opening and ending cutscenes, play-area cards and their triggers.

This appendix implements the storytelling plan of Chapter 8: the Squishy Cup night in the Squishy Corner toy store. World, character and title names remain proposals until the team approves them; durations are production estimates.

## B.1 Presentation

The story is told through two short cutscenes, arena cards and environmental details, reusing existing characters, settings and animation; still images, transitions and simple poses can be combined. Cutscenes are subtitled and skippable. Voice-over is optional; the content stays clear with sound off.

## B.2 Opening cutscene: the store after closing

When: before the first match of a Single Player run. Expected length: 10–12 s.

| Beat | Image and action | Sound / on-screen text |
|---|---|---|
| 1. Closing time | The shutters roll down and the lights dim. The camera glides past shelves of toys to the squishy shelf, where the six fighters blink awake and hop down one by one | Music-box melody, a soft click of the shutter. Text: "Squishy Cup night" |
| 2. In front of the trophy | The Golden Squish shines on the counter beside a hand-drawn map of the five play areas. The group squeezes in for a photo; one touch makes them fall one after another, and the last one rolls a full turn and bounces up | Soft squishes and funny bounces |
| 3. To positions | The squishies look at each other, pop back into shape and roll to the arena island. The wall clock chimes; the title appears | "Six squishies, five play areas. Who will stay on top to the end?" |

The opening shows friendly competition and the group's lovable clumsiness. It names no main character and does not hint at a winner.

## B.3 Play-area introduction cards

Cards appear before the countdown and briefly set the mood and main challenge. Levels are always played in order, but each card still stands on its own because a Multiplayer session may start fresh at L1 with new players. Cards show for 2–3 s with the icon of the main mechanic; controls and live warnings are shown by the interface.

| Code | Level | Introduction line | Image |
|---|---|---|---|
| B02 | Training Ring | "Every Cup starts on the arena island. Watch the rings!" | Arena island with four painted rings and the ring-drop icon |
| B03 | Moving Ground | "Two rings, two directions. Hop over the blocks!" | Turntable, outer ring with obstacle boxes and rails |
| B04 | Cannon Court | "The toy soldiers have loaded their cannons!" | Air and fire cannons on the rim with their warning zones |
| B05 | Frost Ring | "Careful in the freezer: the ice is cracking." | Ice-lolly freezer floor with one tile starting to crack |
| B06 | Chaos Core | "The top shelf is waiting. The Golden Squish is right there!" | Cannons and cracking ice tiles beside the trophy |

## B.4 Ending cutscene: laughing around the Golden Squish

When: after a completed Single Player run. Expected length: 6–8 s.

| Beat | Image and action | Sound / on-screen text |
|---|---|---|
| 1. Cup result | The result appears on the counter. A winner, if any, stands in the place of honour with friends gathered around | Result music and congratulations matching the real result |
| 2. A shared moment | The winner lifts the trophy while the others edge into the frame. Without a single winner, the trophy stands among the group | Cheers, applause or short expressive sounds |
| 3. The familiar fall | A squishy accidentally touches a neighbour. The group rolls into a heap as in the opening, then bounces up laughing as morning light reaches the window, and they hop back onto their shelf | Bounce, shop bell. Text: "See you next Squishy Cup!" |

The ending respects the contest result while friendship continues after the match. The line "Five play areas completed" is used only when the run data confirms all five levels were cleared.

## B.5 Results presentation by mode

| Case | Presentation |
|---|---|
| Single: match won | Short celebration; the match result shows before the next choice |
| Single: out of the match | The fighter falls onto the plush pile and bounces, unharmed. Message: "One fall, one more lesson!" |
| Single: run completed | Ending scene of B.4; congratulations reflect the run actually completed |
| Survival / Rank results | Result board and podium under the selected rule; Rank shows points and lives left; short celebration animation |
| Shared rank | Tied fighters are honoured equally; no single winner is invented |
| Early exit or lost session | A suitable message; no festival-complete scene |

The plush pile catches every falling squishy. Under Survival it appears only after elimination or on the result screen; under Rank it is also the respawn moment, when a fighter with lives left springs back onto the floor after 2 s.

## B.6 Triggers

| Code | Content | Trigger |
|---|---|---|
| B01 | Opening cutscene | A new Single Player run starts, before the first countdown |
| B02–B06 | Play-area cards | The level has loaded, before the countdown |
| B07 | Ring-drop or last-10-seconds warning | L1: the hazard system raises the warning at 42, 72, 102 and 132 s; L2–L5: the match clock reaches 140 s |
| B08 | Single Player match feedback | The match result is confirmed |
| B09 | Single Player ending cutscene | The run is confirmed complete under Section 3.5 |
| B10 | Multiplayer summary | The session ends and the host confirms the score table |

Each event plays once at its moment. In Multiplayer, introductions and summaries follow the shared session flow and never pause one player mid-match.

## B.7 Skipping and transitions

Skipping the opening goes to the goal and controls card before the countdown; skipping the ending goes to the result screen or the next choice. Cutscenes never change points, ranks or completion, and their time never counts on the match clock. In-match warning lines are short, readable and never cover danger zones.
