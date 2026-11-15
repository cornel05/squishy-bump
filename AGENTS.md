# AGENTS.md: Squishy Bump

## What this is
A 3D party brawler for up to 6 squishy fruit fighters (BKU Group 18 course project, A02 deadline **25/11/2026**).
Unity 6000.3.28f1 (URP, C#), Netcode for GameObjects 2.x, **host-authoritative**, Windows x64.
Two devs: Đình Duy (`cornel05`, Claude Code) and Minh Nhật (`ZteHahah`, Codex). Both on Windows.

## Golden rules
1. **GitHub issues are the plan.** Work only on an issue; read its "Tiêu chí hoàn thành" checkboxes: they are hard acceptance criteria.
2. **The GDD is the design truth** (`docs/gdd/`). A deviation needs the `design-change` skill (issue + `docs/design-changelog.md` entry).
3. **Host-authoritative.** Gameplay state changes only on the host; clients send `InputCommand`. Host mode even in Single Player (ADR-0002).
4. **Modules depend only on Core** (ADR-0003). Cross-module talk goes through Core interfaces/events, wired in `App`.
5. **Prove it with a command.** Nothing is done until `tools/verify.ps1` and `python tools/check_repo.py` pass.
6. Stay in your module and your owner's files (see Ownership). Core changes affect both members: tell the teammate what changed.
7. Never force-push, never `git reset --hard` shared work, never commit `Library/` or secrets.

## Repo map
```
AGENTS.md / CLAUDE.md      agent rules (this file) / Claude import
Assets/_Project/
  Scripts/<Module>/        one asmdef per module + AGENTS.md (owner, invariants, issues)
    Core Physics Hazards AI Match Net UI Presentation App Editor
  Tests/EditMode|PlayMode  NUnit tests (SquishyBump.Tests.*)
  Settings/Input/          SquishyControls.inputactions (wrapper: Scripts/Core/Input/SquishyControls.cs)
  Settings/URP/            render pipeline assets
  Data/ Prefabs/ Art/ Audio/
  Scenes/                  Boot MainMenu Lobby Levels/L1..L5 Sandbox/Test_Duy Sandbox/Test_Nhat
Assets/ThirdParty/         external assets, each with LICENSE/CREDITS
docs/gdd/                  GDD v1.3 as Markdown, ids.md = ID index (PDF original alongside)
docs/                      architecture, tech-stack, plan, testing, design-changelog, decisions/ (ADRs)
tools/                     setup-dev, verify, build-windows (.ps1), check_repo.py, githooks/, hooks/, gdd/
.agents/skills/            shared skills (Codex native; Claude via .claude/skills wrappers)
.github/                   PR + issue templates, CODEOWNERS, CI (repo-checks, disabled)
```

## Commands
Run from the repo root. Scripts work in Windows PowerShell 5.1 and pwsh 7.
| Goal | Command |
|---|---|
| Setup (once per clone, idempotent) | `powershell -ExecutionPolicy Bypass -File tools/setup-dev.ps1` |
| Open the project | `unity open .` (or Unity Hub, Editor 6000.3.28f1) |
| Verify: compile + EditMode tests | `powershell -ExecutionPolicy Bypass -File tools/verify.ps1` (add `-PlayMode`) |
| Build Windows x64 (Editor closed) | `powershell -ExecutionPolicy Bypass -File tools/build-windows.ps1` → `Builds/Windows/SquishyBump.exe` |
| Repo rules | `python tools/check_repo.py` (hooks use `--quick`) |
| Live Editor | `unity status`, `unity recompile .`, `unity command test run --mode editor`, `unity command` (lists all). `unity test` works only with the Editor closed |
| Regenerate scenes/build list/input wrapper | Editor menu *Squishy Bump > Scaffold Project* |
| Find a GDD fact | `docs/gdd/ids.md` (by ID) or search `docs/gdd/*.md` |

## Workflow
1. Pick an issue: `gh issue view <n>`. Stop if a hard dep ("⛔ Phải xong") is still open. Skill: `issue-start`.
2. Branch `feat/SB-xx-short-slug` from `main` (`chore/` or `docs/` for non-feature work).
3. Small commits, subject `SB-xx: <imperative>` (or `chore:` / `docs:`). The commit-msg hook enforces it.
4. `tools/verify.ps1` + `python tools/check_repo.py` green. Skill: `verify`.
5. PR from `.github/pull_request_template.md` with `Closes #n`. Skill: `issue-finish`.
6. **Squash merge** your own PR once step 4 is green. Review is optional: ask the teammate (skill `review-pr`)
   when you want a second look. Nothing on GitHub checks the PR (CI is disabled), so step 4 is the only gate.
Detail for humans: `CONTRIBUTING.md`.

## Unity rules for agents
- Never edit `Library/`, `Temp/`, `Logs/`, `UserSettings/` (generated; Claude hooks block it).
- Every file or folder under `Assets/` has its `.meta` committed with it. **Never hand-write GUIDs.** Let the
  Editor generate them: open the project, `unity recompile .`, or run Unity in batchmode once.
- Prefer Editor APIs, editor scripts (`Scripts/Editor`) or the Unity CLI over hand-editing `.unity`/`.prefab` YAML.
  ProjectSettings `*.asset` values may be edited as text, then checked by a test.
- No magic numbers: balance values come from GDD 6.1 via ScriptableObjects in `Assets/_Project/Data/`.
- Gameplay state changes only on the host. Gameplay modules read `InputCommand`, never the keyboard.
- Namespace = assembly name (`SquishyBump.<Module>`). Asmdef references by name, not GUID.
- Do not edit the generated `SquishyControls.cs`; edit the `.inputactions` asset.
- Assets > 20 MB need the team's OK (GitHub LFS quota 1 GB). Binaries must match an LFS pattern in `.gitattributes`.
- Sandbox scenes are personal. Shared scenes are edited by their owner, or changed through prefabs.
- Never hand-edit `.unity`/`.prefab`/`.asset` YAML while an Editor has this project open (its next save overwrites
  you); go through the Editor instead. With it closed, keep every `fileID`/`guid` intact and reopen to confirm it loads.

## Code rules
- Time-based logic uses `Time.fixedDeltaTime` in simulation (60 Hz) and `Time.deltaTime` only in presentation.
- No static singletons for our own game state; pass references via Core interfaces wired in `App`
  (engine singletons such as `NetworkManager.Singleton` are fine).
- The host validates every client message (`InputCommand` ranges, cooldowns, sizes) before using it.
  Each replicated value states reliable/unreliable and its rate (GDD 6.1: input 60 Hz, snapshots 30 Hz).
- State machines (match flow, bot FSM) use explicit states and log transitions behind a debug flag.
- UI and Presentation never modify game state; they raise Core events/commands.
- Tests: class `<System>Tests`, methods `<Scenario>_<Expected>`, arrange/act/assert. A bug fix adds a
  regression test that was seen failing before the fix.

## Definition of done
- Every acceptance checkbox in the issue is satisfied and ticked, with evidence in the PR.
- `tools/verify.ps1` → `VERIFY: PASS`, 0 errors and 0 warnings from `SquishyBump.*` code.
- `python tools/check_repo.py` → `PASS`. Metas committed. No new magic numbers.
- Tests added or updated for new logic; `docs/testing.md` updated if a T-test ran.
- Design change → `docs/design-changelog.md` entry + `design-change` issue.
- PR squash-merged into `main` ("done" = merged). Teammate review is optional.

## Ownership
| Area | Owner |
|---|---|
| Core, App, docs/decisions | both |
| Physics (PlayerMotor, CollisionResolver, FighterProfile), Presentation (RollVisual, CameraRig, audio), Editor, Hazards H01–H05 + H07, Net/Sync, UI/Menus, scenes L2 L3 | Đình Duy `cornel05` |
| AI (BotBrain, A*), Match (MatchDirector, ScoreService), Hazards/IceTiles (H06), Net/Session, UI/HUD (HUD, lobby, results, tutorial), scenes L1 L4 L5 | Minh Nhật `ZteHahah` |
Issue `area:` label → module: physics→Physics, hazard→Hazards, ai→AI, rules→Match, network→Net, ui→UI/Presentation, level→Scenes.
| Build + README | Duy · Credits, change log, test record, playtest report: Nhật |
`.github/CODEOWNERS` only suggests a reviewer; no approval is required to merge. Per-module detail: `Assets/_Project/Scripts/<Module>/AGENTS.md`.

## Where design truth lives
| Question | Look in |
|---|---|
| A GDD ID (PF03, H04, T09, AI-2, O07, …) | `docs/gdd/ids.md` → file#heading |
| Balance numbers | `docs/gdd/06-systems-physics-balance.md` §6.1 |
| Controls | `docs/gdd/03-how-to-play.md` §3.4 |
| Architecture / authority | `docs/gdd/10-technology-architecture.md` §10.3–10.4, `docs/architecture.md` |
| Acceptance tests T01–T20 | `docs/gdd/14-validation-rubric-map.md` §14.3, results in `docs/testing.md` |
| Plan, milestones, roles | GitHub issues + project board; summary `docs/plan.md` |
| Package versions | `docs/tech-stack.md` |
| Changes vs GDD | `docs/design-changelog.md` |

## Agent tooling setup
- **Codex (Nhật):** reads this file and `.agents/skills/` natively. Install the Unity plugin once:
  `codex plugin marketplace add Unity-Technologies/unity-agent-plugin` then `codex plugin add unity@unity-agent-plugin`.
- **Claude Code (Duy):** `CLAUDE.md` imports this file. Project settings in `.claude/settings.json` enable the
  Unity plugin (`unity@unity-agent-plugin`), permissions and hooks (`tools/hooks/`). Skills load from `.claude/skills/`
  (thin wrappers to `.agents/skills/`).
- **Both:** Unity CLI (`winget install Unity.CLI`, then `unity auth login`). `com.unity.pipeline` is in the manifest,
  so `unity pipeline editors` / `unity recompile` / `unity command test run` can drive the open Editor.
- Project rules here override global agent frameworks (CCGS, awesome-gamedev skills). Do not spawn studio hierarchies.
