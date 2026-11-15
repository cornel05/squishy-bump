# Testing

## Commands

| What | Command | Output |
|---|---|---|
| Compile + EditMode tests | `powershell -ExecutionPolicy Bypass -File tools/verify.ps1` | `VERIFY: PASS/FAIL`, results in `TestResults/` |
| + PlayMode tests | `... tools/verify.ps1 -PlayMode` | same |
| Windows build | `powershell -ExecutionPolicy Bypass -File tools/build-windows.ps1` | `Builds/Windows/SquishyBump.exe` |
| Repo rules | `python tools/check_repo.py` (`--quick` in hooks) | `check_repo: PASS/FAIL` |

`verify.ps1` uses the open Editor through the Unity CLI (`unity recompile --strict`, `unity command test run`) when
`Temp/UnityLockfile` exists, otherwise batchmode. Build needs the Editor closed.

Tests live in `Assets/_Project/Tests/EditMode` and `Tests/PlayMode`. Naming: class `<System>Tests`,
method `<Scenario>_<Expected>`, arrange/act/assert.

## Automated tests

| Test | Covers | Last run |
|---|---|---|
| `EngineSettingsTests` (3) | fixed step 1/60, gravity −20, Core dependency rule | 15/11/2026 pass (SB-01–03 bootstrap) |
| `ConfigurationAssetTests` (10) | SB-05: 5 cấu hình sàn L1–L5 và 5 cấu hình bot D1–D5 theo GDD A.5 | 15/11/2026 · `40559a9` · Minh Nhật · `tools/verify.ps1`: 13/13 EditMode đạt, 0 lỗi, 0 cảnh báo. Chi tiết: `TestResults/EditMode.xml`. |

## Acceptance test record (GDD 14.3, T01–T20)

Definitions: [`gdd/14-validation-rubric-map.md`](gdd/14-validation-rubric-map.md#143-priority-acceptance-tests)
and [`gdd/ids.md`](gdd/ids.md). Update a row whenever the test is run (date, build/commit, who, result, evidence).

| ID | Status | Date | Commit | By | Evidence / notes |
|---|---|---|---|---|---|
| T01 | not run | | | | |
| T02 | not run | | | | |
| T03 | not run | | | | |
| T04 | not run | | | | |
| T05 | not run | | | | |
| T06 | not run | | | | |
| T07 | not run | | | | |
| T08 | not run | | | | |
| T09 | not run | | | | |
| T10 | not run | | | | |
| T11 | not run | | | | |
| T12 | not run | | | | |
| T13 | not run | | | | |
| T14 | not run | | | | |
| T15 | not run | | | | |
| T16 | not run | | | | |
| T17 | not run | | | | |
| T18 | not run | | | | |
| T19 | not run | | | | |
| T20 | not run | | | | |
