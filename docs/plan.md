# A02 implementation plan (summary)

English summary of [`IMPLEMENTATION_PLAN_A02.md`](IMPLEMENTATION_PLAN_A02.md) (Vietnamese original, 14/11/2026).
The issues on GitHub are the live plan; this page only explains how they fit together.

**Deadline:** 25/11/2026. **Start:** no code. **MVP:** GDD v1.3 section 13.4.

## Milestones (due 23:59)

| Milestone | Due | Exit condition |
|---|---|---|
| M0 Setup | 15/11 | Repo + Unity project + LFS/YAMLMerge, ScriptableObject data, shared interfaces |
| M1 Core loop | 17/11 | One L1 match from countdown to result (1 human + 5 D1 bots) |
| M2 Network & rules | 19/11 | 2+ PCs on LAN finish an L1 match with identical results; disconnect does not crash; Rank works |
| M3 Hazards L2–L5 & bots | 21/11 | All 5 levels; T09 passes for H01–H05 + H07; T10 passes; bots D2–D5 |
| M4 Freeze & playtest | 23/11 | All P0 tests run and recorded; playtest done; no blocking bugs |
| M5 Submission | 25/11 | Build runs per README; credits, change log, test record, playtest report |

Most important technical rule: **all gameplay runs in NGO host mode from M1, including Single Player** (ADR-0002).

## Roles (rebalanced, recorded as D33)

| | Đình Duy (`cornel05`) | Minh Nhật (`ZteHahah`) |
|---|---|---|
| Core systems | PlayerMotor, CollisionResolver/Wobble, FighterProfile, RollVisual/Camera | MatchDirector, Survival/Rank rules, ScoreService, knockout |
| Network | Host-authoritative sync (input/snapshot), hazard events, prediction | SessionManager (LAN host/join), match events, disconnect |
| Hazards / levels | H01, H02, H07, H03, H04, H05; scenes L2, L3 | H06 ice tile (BFS); scenes L1, L4, L5 |
| AI | — | BotBrain FSM, danger map, A*, D1–D5 |
| UI | Menu, settings, arena card, banner, audio, save | HUD, 3D lobby, results/scoreboard/spectator, tutorial |
| Submission | Build + README | Credits, change log, test record, playtest report |

## Tiers and cut order

- `tier:A-gate`: the "build runs per README" gate and the core loop. Never cut.
- `tier:B-mvp`: the rest of MVP 13.4.
- `tier:C-full`: full scope (Relay, cutscenes, rebinding, Vietnamese UI) and cut candidates.

If still late on 21/11, cut in this order (both agree; each cut is a change-log entry):
1. Client prediction (SB-22). 2. 3D walking lobby → 2D lobby (SB-25).
3. Class tactics for bots D3–D5 (part of SB-47). 4. Playtest reduced to one round (SB-53).

## Board and process

- Status: Todo → In Progress → Done. WIP limit 2 per person. Hard deps use GitHub "blocked by".
- One branch per issue `feat/SB-xx-slug`, PR with `Closes #n`, the other member reviews, squash merge.
- Rules: interfaces first (SB-06); "done" = merged to main; one personal test scene each; stuck for
  half a session → label `blocked`, tell the other, switch issue. 10-minute sync every evening.
- Deviating from the GDD → issue labelled `design-change` + entry in [`design-changelog.md`](design-changelog.md).

## Changes after planning

- SB-02 and SB-03 done by Duy in the shared M0 bootstrap, reviewed by Nhật (D34).
- Agent rules live in `AGENTS.md`; `CLAUDE.md` only imports it. Shared skills live in `.agents/skills/`.
