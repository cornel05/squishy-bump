# Squishy Bump

A 3D party brawler for up to six squishy fruit fighters: bump each other off living arenas, on one PC
against bots or with friends over LAN. Course project of BKU Group 18 (Đình Duy, Minh Nhật).

> Skeleton of the A02 README (GDD 14.5). Sections marked *TBD* are filled during M1–M5.

## Requirements
- Windows 10/11 x64, keyboard and mouse.
- To play: nothing else (the build is self-contained). *Download link: TBD (SB-60).*
- To develop:
  - **Unity 6000.3.28f1** (Unity 6.3 LTS, pinned in `ProjectSettings/ProjectVersion.txt`), with Unity Hub or the Unity CLI
    (`winget install Unity.CLI`, then `unity install 6000.3.28f1`). A Unity Personal licence must be active.
  - Git + Git LFS, Python 3.10+, GitHub CLI (`gh`), PowerShell 5.1 or 7.

## First-time setup
```powershell
git clone https://github.com/cornel05/squishy-bump.git
cd squishy-bump
powershell -ExecutionPolicy Bypass -File tools/setup-dev.ps1
```
`setup-dev.ps1` pulls LFS files, finds the matching Editor, registers UnityYAMLMerge for scenes/prefabs, and
installs the git hooks. It is safe to run again.

## Open in Unity
`unity open .` or Unity Hub → *Add project from disk* → the repo folder. Start from `Assets/_Project/Scenes/Boot.unity`.

## Run tests
```powershell
powershell -ExecutionPolicy Bypass -File tools/verify.ps1            # compile + EditMode tests
powershell -ExecutionPolicy Bypass -File tools/verify.ps1 -PlayMode  # + PlayMode tests
python tools/check_repo.py                                           # repo rules
```
Results: `TestResults/`. Acceptance test record: [`docs/testing.md`](docs/testing.md).

## Build
Close the Editor, then:
```powershell
powershell -ExecutionPolicy Bypass -File tools/build-windows.ps1
```
Output: `Builds/Windows/SquishyBump.exe`.

## LAN quick-start
*TBD (SB-20).* The host picks *Host* and shares their LAN IP; others pick *Join* and enter it. Port **7777/UDP**
(Unity Transport default, GDD 10.2) must be allowed through Windows Firewall on the host.

## Controls (GDD 3.4)
| Input | Action |
|---|---|
| W A S D | Move (camera-relative) |
| Mouse | Orbit camera |
| Space | Jump (only when grounded) |
| Left Shift or left mouse | Dash (3.0 s cooldown) |
| Tab (hold) | Scoreboard |
| Esc | Menu (pauses only in single-player) |

## Known limits
*None recorded yet.*

## Documentation
- [`AGENTS.md`](AGENTS.md): rules for AI agents and the dev workflow · [`CONTRIBUTING.md`](CONTRIBUTING.md)
- [`docs/gdd/`](docs/gdd/README.md): Game Design Document v1.3 ([ID index](docs/gdd/ids.md))
- [`docs/architecture.md`](docs/architecture.md) · [`docs/tech-stack.md`](docs/tech-stack.md) · [`docs/plan.md`](docs/plan.md)
- [`docs/testing.md`](docs/testing.md) · [`docs/design-changelog.md`](docs/design-changelog.md) · [`docs/decisions/`](docs/decisions/)
