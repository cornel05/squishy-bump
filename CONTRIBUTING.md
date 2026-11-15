# Contributing

Short version for humans. Agents: the same rules, in more detail, are in [`AGENTS.md`](AGENTS.md).

## Branches, commits, PRs
- One issue = one branch: `feat/SB-xx-short-slug` (e.g. `feat/SB-10-player-motor`). Use `chore/` or `docs/`
  for non-feature work. Branch from an up-to-date `main`.
- **Small commits**, one logical change each. Subject: `SB-xx: <imperative>` (e.g. `SB-10: add dash cooldown`);
  `chore:` / `docs:` for housekeeping. The `commit-msg` hook rejects other subjects.
- Open a PR with the template, write `Closes #<issue number>` (issue numbers differ from SB ids: SB-01 = #2).
- Review is optional: you may squash merge your own PR. Ask the other member when you want a second look.
  Squash merge only; the branch is deleted on merge.
- Before merging (CI is disabled, so this is the only gate): `tools/verify.ps1` and `python tools/check_repo.py` both pass.

## Scenes and prefabs
- Each member only edits the scenes they own:
  - Duy: `Levels/L2`, `Levels/L3`, `Sandbox/Test_Duy`
  - Nhật: `Levels/L1`, `Levels/L4`, `Levels/L5`, `Sandbox/Test_Nhat`
  - `Boot`, `MainMenu`, `Lobby`: agree in the issue who edits; one person at a time.
- **Shared parts are prefabs.** Put a fighter, hazard, UI panel, etc. in `Assets/_Project/Prefabs/` and edit the
  prefab, not its instance in someone else's scene.
- Try things in your own sandbox scene, never in a shared one.
- Scene/prefab merges use UnityYAMLMerge (registered by `tools/setup-dev.ps1`). If a merge still conflicts,
  keep one side and redo the other change in the Editor.

## Stuck or changing the design
- Stuck for more than half a work session: label the issue `blocked`, tell the other member, switch issue.
- Need to differ from GDD v1.3: open a `design-change` issue and add an entry to `docs/design-changelog.md`.

## First-time setup
See [README.md](README.md#first-time-setup).
