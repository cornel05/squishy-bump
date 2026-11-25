# Appendix E. Playtest plan and forms

> **In this chapter:** How playtests are run, recorded and turned into changes.

This appendix defines how Squishy Bump playtests are run, how feedback is collected and how changes are recorded. The criteria are targets to verify, not results achieved.

## E.1 Test goals

| Code | Area | Question to answer |
|---|---|---|
| E01 | Rules | Do players understand the goal of pushing opponents off, what elimination means and, under Rank, how lives and points work? |
| E02 | Controls | Are movement, jump and dash easy to learn and perform? |
| E03 | Fighter weight classes | Do players tell the three classes apart and play differently with each? |
| E04 | Living Arena | Do players notice warnings and understand the danger coming? |
| E05 | Level | Does each arena convey its main challenge and give time to react? |
| E06 | UI and results | Are the HUD, spectating and scoreboard easy to understand? |
| E07 | Multiplayer | Do all machines show the same eliminations, results and session score? |
| E08 | Experience | Is the match fun, easy to follow and makes players want another one? |

## E.2 Participants and rounds

Two playtest rounds with at least eight people outside the team in total, preferably people who have not read the GDD or played a build.

| Round | Content | Purpose |
|---|---|---|
| Round 1: familiarisation | Tutorial, Single Player and the first levels | Find problems with controls, rules, classes and warnings |
| Round 2: full experience | All available arenas and Multiplayer with up to six players under both rules | Evaluate pacing, readability, session results and the changes after round 1 |

Formal Multiplayer tests use six humans on six PCs. LAN and Internet are recorded separately, and Internet tests need at least two different networks. Sessions with fewer players are internal checks only.

## E.3 Session procedure

- Prepare: record build, machines, connection, match setup and the changes to check.
- Introduce: explain the purpose and that the team is evaluating the game design.
- First play: participants use the in-game tutorial; help only when they cannot continue, and record each help.
- Target situations: observe class match-ups, hazards, ring-outs, respawns and results.
- Feedback: participants fill in the form before any group discussion.
- Summarise: compare comments with observations, logs and images; decide what to retest.

Participants receive anonymous codes. Video or audio is recorded only with consent.

## E.4 Test scenarios

| Code | Situation | Observations |
|---|---|---|
| PT01 | A newcomer completes the Training Ring tutorial | Time, mistaken steps, help needed, can restate the goal |
| PT01b | Pick a fighter in the 3D lobby and get ready | Finds the stands and the ready step without help; understands the class badge |
| PT02 | Approach, collide and hold position | Direction control, understanding of push, awareness of the edge |
| PT03 | Play the same level with a Heavy and a Light fighter | Notices the difference in push and speed; reasons for fighter choice |
| PT04 | Play Moving Ground | Reads the motion, adjusts the path and braces for the reversal |
| PT05 | Face air and fire cannons | Sees the warning, finds the danger zone and reacts in time |
| PT06 | Face cracking tiles | Notices the warning state and leaves the tile |
| PT07 | Play Chaos Core | Reads several signals at once; notes effects or angles that hide information |
| PT08 | The rings drop on L1, or the clock reaches the last 10 s on L2–L5 | Notices the warning; understands why space shrinks on L1 and how survivors are ranked at the time limit |
| PT09 | A player falls under each rule | Survival: understands they are spectating. Rank: understands the respawn, the lost life and who scored |
| PT10 | A Multiplayer session ends | Understands ranks, points or knockouts; same result on every machine |
| PT11 | One round with sound off | Still reads classes, lives, cooldowns and hazard warnings visually |
| PT12 | Replay after a match | Clear continuation; no leftover state or interface from the previous match |

## E.5 Observation sheet

| Session field | Entry |
|---|---|
| Session code / date | … |
| Build version | … |
| Observer | … |
| Participant code | … |
| Mode / rule | … |
| Level / chosen fighter | … |
| Machine / connection | … |

| Time | PT code | Observed action or statement | Help given | Evidence |
|---|---|---|---|---|
| … | … | … | … | … |

Record concrete behaviour, such as "walked off a band during the ring-drop warning", instead of general remarks such as "the player did not understand". Interpretations are written separately so that inference is not mixed with observation.

## E.6 Player feedback form

Rate on a scale of 1–5, where 1 is strongly disagree and 5 is strongly agree. "Not experienced yet" can be chosen for content not reached.

| Code | Statement | Score |
|---|---|---|
| Q01 | I understand the goal and how elimination works | … |
| Q02 | I move, jump and dash as I intend | … |
| Q03 | I can tell Heavy, Normal and Light fighters apart | … |
| Q04 | My fighter's weight class changed how I played | … |
| Q05 | I notice warnings before hazards hit | … |
| Q06 | I can clearly see the fighters, the edge and danger zones | … |
| Q07 | I understand why I was pushed or eliminated | … |
| Q08 | I understand the results board and my rank | … |
| Q09 | The match felt fair | … |
| Q10 | I want to play another match | … |

- Which moment was the most fun?
- What was hardest to understand or control?
- Why did you pick your fighter, and would you pick another?
- If you could change one thing, what would it be?

## E.7 Metrics and criteria

These thresholds are proposed targets, aligned with Section 14.2.

| Metric | How it is measured | Target |
|---|---|---|
| Rule understanding | Newcomers restate the goal and elimination condition | At least 80% correct after the tutorial |
| Tutorial time | From start to completion; skips recorded separately | Median at most 90 s |
| Warning reading | Players point to the danger zone in test situations | At least 80% correct, per hazard |
| Perceived fairness | Q09 | Median 4/5 or higher |
| Wish to replay | Q10 | Median 4/5 or higher |
| Multiplayer consistency | Compare results and points on all machines | No mismatch in tested sessions |
| Blocking bugs | Bugs that stop starting, continuing or getting a result | None in the acceptance flow |

A successful dodge is not the only proof that a warning was read: a player may understand the danger but react late. Reports separate perception, decision and execution.

## E.8 Summary and retest

| Issue ID | Evidence / frequency | Priority | Proposed change | Retest result |
|---|---|---|---|---|
| … | … | … | … | … |

Priority goes to match-blocking bugs, wrong results and desync, then to controls, warnings and UI, and finally to presentation. Changes are logged in Appendix D with the build used for the retest. Reports state the number of people and matches, the conditions and the limits of the data, and never generalise one session into a balance conclusion for the whole game.
