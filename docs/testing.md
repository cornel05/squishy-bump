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
| `TrainingRingTests` (2) | SB-14: nạp scene L1, bốn collider và sáu spawn, hai điều kiện ring-out và chống báo trùng đến khi hồi sinh | 15/11/2026 · `1343ee1` · Minh Nhật · `tools/verify.ps1 -PlayMode`: 3/3 EditMode và 2/2 PlayMode đạt. Báo cáo: `TestResults/PlayMode.xml`. |
| `SurvivalMatchTests` (7), `SurvivalArenaTests` (1) | SB-16: trạng thái, giới hạn giờ, xếp hạng, quyền host, xác nhận kết quả và ring-out cùng tick qua scene L1 | 15/11/2026 · Minh Nhật · `tools/verify.ps1 -PlayMode`: 10/10 EditMode, 3/3 PlayMode, 0 cảnh báo biên dịch. |
| `ConfigurationAssetTests` (10) | SB-05: 5 cấu hình sàn L1–L5 và 5 cấu hình bot D1–D5 theo GDD A.5 | 15/11/2026 · `40559a9` · Minh Nhật · `tools/verify.ps1`: 13/13 EditMode đạt, 0 lỗi, 0 cảnh báo. Chi tiết: `TestResults/EditMode.xml`. |
| `HostGameplayTests` (1) | SB-06: Boot khởi động Single Player bằng NGO host, chỉ chuyển input hợp lệ đến motor trong phiên host và dừng mô phỏng khi rời phiên | 15/11/2026 · `3dabaf0` · Minh Nhật · `tools/verify.ps1 -PlayMode`: 3/3 EditMode, 1/1 PlayMode đạt. Báo cáo: `TestResults/PlayMode.xml`. |
| `SharedContractTests` (6), `HostGameplayTests` (4) | SB-07: `InputCommand` kẹp move và yaw (regression: vector đơn vị ở góc 38° từng bị loại), payload của `MatchEvent`/`HazardEvent`, host clock chỉ tick khi đang host, hub bỏ event trùng ID, Join từ chối tham số sai, lý do kết thúc phiên | 15/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 26/26 EditMode, 7/7 PlayMode đạt, 0 lỗi, 0 cảnh báo. |

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
| T07 | Một phần | 15/11/2026 | Nhánh SB-16 | Minh Nhật | EditMode kiểm chứng chính sách Survival L2–L5 tại 150 giây. L1 dùng mẫu rơi được cấp trong PlayMode. Chờ chạy đủ năm scene với cả hai luật và H01. |
| T08 | Đạt trong PlayMode | 15/11/2026 | Nhánh SB-16 | Minh Nhật | `SurvivalArenaTests.T08_LastTwoFallOnSameTick_DrawAndRetrySameSeed` đưa hai quả qua kill plane L1 cùng tick. Cả hai hạng 1, chơi lại cùng roster và seed. |
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
