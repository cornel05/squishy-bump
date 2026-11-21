# Testing

## Lượt thử SB-20 ngày 21/11/2026 — phục hồi phiên LAN

Nhánh `feat/SB-20-lan-session-recovery`, base `39bfc7f`: `tools/verify.ps1 -PlayMode`
→ VERIFY: PASS (0 warning, EditMode 162/162, PlayMode 36/36), `python tools/check_repo.py` → PASS.

- `Join_UnresponsiveHost_ReturnsConnectFailedWithinSevenSeconds`: cổng UDP loopback không phản hồi,
  client về Idle với `ConnectFailed`; ca thử hoàn tất trong 5,19 s.
- `LanHostLeaves_ClientInLevelReturnsToMenuWithHostLostMessage`: hai peer NGO qua
  `127.0.0.1:7777`. Khi host đóng phiên, client đang ở L1 về MainMenu và hiện thông báo
  `HostLost` trên trang Multiplayer. Ca thử nạp L1 cục bộ vì hai peer cùng chạy trong
  một tiến trình Unity.

Chưa chạy trên hai máy vật lý hoặc thử mất kết nối đột ngột. Timeout khi không nhận
gói tin của phiên đã kết nối vẫn là 30 s.

## Lượt thử SB-20 ngày 20/11/2026

Nhánh `feat/SB-20-lan-session`, base `b856485`: `python tools/check_repo.py` đạt;
`git diff --check` đạt. Đã bổ sung ca kiểm tra policy approval 2–6 người, version/payload
không hợp lệ, phòng đầy, Single Player và mã từ chối; PlayMode kiểm tra IPv4/port sai
không mở transport. Các ca mới chưa chạy: `tools/verify.ps1 -PlayMode` ngoài sandbox
thoát 198 ở cả hai lượt, `No valid Unity Editor license found`, không có XML kết quả.
Chưa biên dịch, chưa kiểm tra LAN/loopback, chưa có build Windows mới. Không đổi T06/T11/T12
hoặc đánh dấu issue đã đạt. Lỗi môi trường không được ghi như một test gameplay thất bại.

Cập nhật 20/11/2026 (Đình Duy, license OK): gộp #87 + #88 + #89 trên `b856485`, `tools/verify.ps1 -PlayMode`
→ VERIFY: PASS (compile 0 warning, EditMode 162/162, PlayMode 33/33); `check_repo` PASS. LAN/loopback vẫn chưa chạy.

## Lượt kiểm tra môi trường 20/11/2026 — SB-61

Nhánh `feat/SB-61-credits-ai`, base `b856485`: `python tools/check_repo.py` đạt,
CSV nguồn asset có 48 bản ghi (43 tệp có hash SHA-256 khớp và 5 nhóm runtime/tích hợp),
không trùng ID/đường dẫn và không có trường trống. Đây là kiểm tra hồ sơ nguồn.
`tools/verify.ps1` với `UNITY_EDITOR=D:\Tool\Unity\6000.3.28f1\Editor\Unity.exe`
chạy ngoài sandbox nhưng thoát 198: `No valid Unity Editor license found`.
Không có `EditMode.xml`, chưa chạy compile/test; dòng `Compile: OK (0 warnings)` của script
không phải bằng chứng biên dịch trong lượt này. Chưa chạy PlayMode Credits hoặc build Windows.
Không đổi trạng thái T01–T20 vì lượt kiểm tra bị chặn trước khi thực thi test.
## SB-18 — kiểm tra source bổ sung 20/11/2026

Nhánh `feat/SB-18-bot-fsm`: source module và source test đã biên dịch bằng Roslyn đi
kèm Unity 6000.3.28f1 với DLL đang có ở checkout chính. Lệnh từ project cha:
`python scratch/compile_unity_sources.py squishy-bump-sb18`; log tại
`../scratch/csc/squishy-bump-sb18/`. Đây không phải lượt test Unity.
Đã bổ sung 6 ca EditMode cho FSM/authority, delay/seed/retarget, anti-stuck, tiến độ
thực không bị hiểu nhầm là kẹt, né mép và charge/recover. Chưa chạy các ca này.
Unity thiếu license, chưa sinh hai meta hoặc chạy ProjectScaffold để nối D1 vào
MatchSetup. Chưa có PlayMode bot hoặc build Windows; không đánh dấu SB-18 hay T13 đạt.

## SB-18 — kiểm thử Native 21/11/2026

Nhánh `feat/SB-18-bot-fsm`, base `c293fe1`, Unity 6000.3.28f1. Editor entry
`SquishyBump.Editor.ProjectScaffold.ConfigureBotDefaults` chạy thành công, nối D1 vào
MatchSetup qua SerializedObject/AssetDatabase và sinh meta. Không sửa GUID/asset bằng tay.

`tools/verify.ps1 -PlayMode` đạt **VERIFY: PASS**, **171 EditMode / 37 PlayMode**,
0 failed, 0 skipped, 0 warning C#. Kết quả tại `TestResults/EditMode.*` và
`TestResults/PlayMode.*`.

- Sáu `BotBrainTests` kiểm tra authority, bảy state, delay 0,5 s, seed, retarget đủ
  chu kỳ 4 s, anti-stuck, phân biệt tiến độ thật, edge margin D1 và Charge/Recover.
- `BotArenaTests.SinglePlayer_D1BotsMoveThroughPlayerMotorAndEnterEliminatedOnFall`
  chạy Boot → L1, tạo 1 người/5 bot D1 từ MatchSetup thật, chờ GO, đo chuyển động ngang
  từng bot sau 2 s (> 0,15 m), xác nhận Rigidbody động và PlayerMotor; đưa một bot qua
  kill plane rồi kiểm tra Out + state Eliminated. Ca đạt trong 9,261878 s thực.
- `python tools/check_repo.py`: PASS (0 problems); `git diff --check`: sạch.
- `tools/build-windows.ps1`: **BUILD: PASS**, Windows x64 v0.1.0, 100 MB, 0 lỗi,
  1 phút 47 giây. Build tại `Builds/Windows/SquishyBump.exe`; SHA-256 của assembly
  `SquishyBump_Data/Managed/SquishyBump.AI.dll`:
  `900EC2CA68AB21D5330ABA18C15238F9C3B4D968C8442D3D0BB600649FB70F81`.

Đây là kiểm chứng khung D1 và tích hợp input/motor, không phải bài ổn định 120 s,
thống kê AI-1–AI-6 hay T13 đầy đủ năm bậc. Không nâng các test đó thành đạt.

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
| `BalanceProfileTests` (20) | SB-04/SB-19: `BalanceProfile`, 3 `ClassProfile`, 6 `FruitDefinition` assets match GDD 6.1 / 12.2; design checks of GDD 14.1 (slide distances, match end, class responsiveness, jump vs box, snapshot load) | 16/11/2026 · Đình Duy · `tools/verify.ps1`: 46/46 EditMode pass, 0 errors, 0 warnings. |
| `PlayerMotorTests` (20), `FighterTests` (8) | SB-10/SB-11: traction rule per class, ice, air control 30%, surface velocity, jump launch speed, dash Δv = J/m, host cooldown reject, one air dash per airtime, jump → dash → move order, stun input lock and 0.1 s buffer; six fighter prefabs get mass/radius/colour/v_max of their class, real jump apex 1.2 m and air time 0.69 s on a PhysX floor | 16/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 66/66 EditMode, 15/15 PlayMode pass, 0 errors, 0 warnings. |
| `CollisionResolverTests` (14), `CollisionTests` (4) | SB-12: impulse formula of GDD 6.3 with K(h) and dash bonus (worked example, all classes, h = 0 and 5), knockback cap, 80% overlap correction beyond the 0.01 m slop by inverse mass, hit threshold 30 N·s, all 15 pairs tested; on prefabs: T02 head-on, stun 0.15 + 0.02h, PhysX ignores fighter pairs, Wobble decay per 6 s and cap 10 | 16/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 80/80 EditMode, 19/19 PlayMode pass, 0 errors, 0 warnings. |
| `PresentationTests` (4), `PresentationPlayTests` (3) | SB-13: `PresentationProfile` holds squash 1.15 × 0.85 / 0.12 s, camera 9 m, pitch 15–60° (default 35°); rolling without slipping ω = (ŷ × v)/r; squash eases back to 1; on the Apple prefab the Body rolls about +x when moving +z while the collider root never rotates, and a landing squashes only the visual; CameraRig orbits at 9 m, clamps pitch, shake turns off | 16/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 84/84 EditMode, 22/22 PlayMode pass, 0 errors, 0 warnings. |
| `RingDropTests` (6), `RingDropPlayTests` (1), `MatchHostTests` (2) | SB-15: H01 drops at host ticks 2700/4500/6300/8100 (45/75/105/135 s), warning published 180 ticks (3 s) ahead carrying the drop tick, amber then red at 2 Hz in the last 1.0 s, seam on each band's inner edge; on the TrainingRingFloor prefab a fruit on the outer band falls when it drops while the next band holds; host loop on L1 spawns at the level spawns, runs the countdown on host ticks and confirms the result after a ring-out; Test_Duy sandbox hosts and spawns six fruits | 16/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 90/90 EditMode, 25/25 PlayMode pass, 0 errors, 0 warnings. |
| `RespawnPickerTests` (7) | SB-28: safe floor radius = remaining L1 floor minus a band dropping within 3 s minus the 1.5 m edge margin; chosen point stays inside it, outside every active hazard zone and as far as possible from other fruits; no point when no floor is left | 16/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 97/97 EditMode, 25/25 PlayMode pass, 0 errors, 0 warnings. |
| `SyncTests` (9), `MatchHostTests` (+1 assert) | SB-21: input packet carries the last 3 commands and round-trips; short, oversized, >3-command and bad-button packets are rejected; move/yaw clamped on read; host inbox uses each sequence once, drops NaN, caps a flood at 6 (oldest dropped), repeats held buttons without steering when starved; snapshot round-trips all fields and rejects a wrong length; buffer renders 100 ms behind and interpolates; stale snapshots ignored; six-fighter snapshot = 209 bytes. Two-instance LAN run not yet done. | 16/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 106/106 EditMode, 25/25 PlayMode pass, 0 errors, 0 warnings. |
| `HazardSyncTests` (3) | SB-23: hazard event (id, host tick, kind, index, aim, direction) round-trips; wrong length and unknown kind rejected; client telegraph offset = render host time − (effective tick × dt − lead), negative when rendering behind. Two-instance LAN run not yet done. | 16/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 109/109 EditMode, 25/25 PlayMode pass, 0 errors, 0 warnings. |
| `MenuFlowTests` (2) | SB-31: Boot carries `MatchSetup`; main menu builds with Main / Multiplayer / Settings / Credits pages; Single Player hosts and loads L1 through NGO's scene manager and starts the match; settings changes reach the camera (shake off, distance × 1.5 = 13.5 m); Esc pauses Single Player (timeScale 0) and resumes; Leave returns to the main menu with the session Idle; host status text | 16/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 109/109 EditMode, 27/27 PlayMode pass, 0 errors, 0 warnings. |
| `MovingGroundTests` (7), `MovingGroundPlayTests` (2), `MenuFlowTests.L2_StartsWithMovingGroundHazard` | SB-40: H02/H07 values of GDD 7.2; turntable ±20°/s reversing every 15 s with a 2 s ramp, analytic angle = integral of the rate; ring always −15°/s against it; arrows amber 2 s before reversal; v_surface = ω × r_c (1.4 m/s at 4 m); box impulse 50 N·s · K(h) along normalize(t̂ + 0.5 r̂); on PhysX an idle Apple on the disc is carried ≈ 40° in 2 s; a box hits once, re-hits only after 0.5 s, +1 Wobble each; L2 loads from the launcher with six fruits standing on the floor | 16/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 116/116 EditMode, 30/30 PlayMode pass, 0 errors, 0 warnings. |
| `PusherBlocksTests` (3), `PusherBlocksPlayTests` (1) | SB-41: H03 values of GDD 7.2; a pass runs dock to dock (±9.5 m) at 3 m/s with 0.3 s eases (6.63 s); impulse 50 N·s · K(h) along the lane; 40 s on PhysX with H07 turning: first departure ≥ 5 s, A leaves west and B east, each waits ≥ 2 s at a dock, departures announced 1 s ahead, and no block ever overlaps an H07 box (exact shapes) | 16/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 119/119 EditMode, 31/31 PlayMode pass, 0 errors, 0 warnings. |
| `CannonsTests` (7), `CannonsPlayTests` (1), `MenuFlowTests.Level_StartsWithItsHazard(3)` | SB-42/43: H04/H05 values of GDD 7.3; sweep ±60° at 30°/s; shots every 5 s (4 s Chaos) staggered 1.25 s; gust F₀(1 − d/L)·K(h), J = 90 N·s at the muzzle; blast J₀(1 − d/R)·K(h) outward with 30% up; v₀ = 15.5 m/s and t = 1.10 s for 12 m, fixed-step flight lands within 0.3 m; L3 layout on host ticks: aim event 1 s ahead carries the fire tick, fire cannon locks the fruit in its 10–16 m band and lands within 0.4 m, +2 Wobble from the blast and +1 per second in the patch, air gust ≈ 45 N·s at 5 m with +1 Wobble; L3 loads from the launcher | 16/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 127/127 EditMode, 33/33 PlayMode pass, 0 errors, 0 warnings. |
| `MatchOverlayTests` (2), `MenuFlowTests` (card assert) | SB-48: `MatchSetup` holds the B02–B06 card lines, card 2.5 s, 10 s final warning; on L1 from the menu the arena card shows and the countdown waits for it; B07 due at 140 s on L2–L5 only (L1 warns on each ring drop) | 16/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 129/129 EditMode, 33/33 PlayMode pass, 0 errors, 0 warnings. |
| `AudioFeedbackTests` (3), `MenuFlowTests` (audio assert) | SB-49: twelve GDD 12.7 placeholders exist as 44.1 kHz mono WAVs and are wired in Sfx order; cannon charge ≥ 1.0 s, ring creak ≥ 3 s; hit-stop 40 ms, squish pitch rises with impulse and caps; light hit → bump, strong hit → squish with pitch > 1 on L2/L3; amber #FFB703 / red #E63946 | 16/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 132/132 EditMode, 33/33 PlayMode pass, 0 errors, 0 warnings. |
| `LocalSaveTests` (5) | SB-51: save → load keeps settings, binding overrides, last fruit, best Survival level and best Rank score; replacing a save leaves no temp file; records only improve; missing file → defaults; corrupt file kept as `.bad` with defaults; the JSON holds no match state | 16/11/2026 · Đình Duy · `tools/verify.ps1 -PlayMode`: 137/137 EditMode, 33/33 PlayMode pass, 0 errors, 0 warnings. |
| `FighterVisualTests` (18) | SB-11: each of the six fighter prefabs has its A15 .glb (`tools/models/make_fruits.py`, Blender 4.2) under `Visual/Body/Model`, the placeholder sphere hidden; the only collider is the root sphere at the class radius; the model body is 2r wide (oval fruits within 15%) |
| `HitStopTests` (1) | SB-21 LAN bug: a hit-stop that lands while the host's Esc menu is open (LAN, does not pause) gives time back; before the fix the host stayed at `timeScale` 0 and both windows froze when two fruits touched. Seen failing (`Expected 1, But was 0`) before the fix |

## L3 frame-time baseline for SB-52 (#43)

21/11/2026 · Codex · Windows x64 v0.1.0, commit `2bf7cc3` · Intel Core i7-12700H,
RTX 4050 Laptop GPU, PC quality, 1920 × 1080. The build script succeeded. Ran
`SquishyBump.exe -perf 30 -screen-width 1920 -screen-height 1080 -screen-fullscreen 0`
and selected **Single Player → L3 Cannon Court**. The recorder starts on entering
the level, so the sample includes the opening countdown.

- 30.0003 s, 23,313 frames, zero overwritten. Median **869.79 FPS**, p95 **1.803 ms**.
  Both metrics were recomputed independently from the CSV and matched the summary.
- Capture files: `Application.persistentDataPath/perf/20261120T175926004Z-L3-2cf6265a5ca34ceb8afc2903b6832a78.{csv,txt}`.
  The file timestamp is UTC.
- `tools/verify.ps1 -PlayMode`: 165 EditMode and 36 PlayMode tests, zero compile
  warnings. `FrameStatsTests` covers median and p95, ring wrap, and GDD thresholds.
- Use L5 Chaos Core with the prescribed roster and measure bot logic before updating T14.
  This L3 run measured frame time only. Some objects at the edge of the scene appeared magenta in the Windows player.

## T15 Multiplayer Play Mode evidence (#45)

21/11/2026 · Codex · Unity 6000.3.28f1, project v0.1.0, commit `4f0c0de`.
Main Editor hosted L3 Cannon Court with one Multiplayer Play Mode clone on
`127.0.0.1:7777`. The other four fighter slots were idle. The host's Multiplayer
Tools Network Simulator applied 50 ms packet delay in each direction, zero
configured jitter, and 2% packet loss. On the instrumented client run, Unity
Transport reported 146 ms RTT despite the nominal 100 ms setting.

- In the instrumented run, all six alternating A/D inputs moved the client's fruit.
  From queuing input to 4.5 cm of visible horizontal displacement took **344–354 ms**.
  The host logged 162 distinct movement commands. The client stayed joined and
  displayed six fighters. The immediate-response criterion failed, consistent with
  the absence of client-side prediction in D35.
- A separate run moved the host fruit with all six inputs (99–110 ms by the same
  displacement measure). In that run the client did not move within the 420 ms
  observation windows. The probe did not yet confirm keyboard state or command
  arrival on that run, so it cannot establish a gameplay freeze.
- Host and client logs matched for all 71 captured cannon events. The first six
  visible gust-cone onsets matched in direction and rounded rendered host tick.
  By wall clock the client displayed them 148–161 ms after the host.

The test used a temporary Unity Input System probe. Local evidence is in
`TestResults/T15-host.txt`, `TestResults/T15-client.txt`, and the corresponding
`-run1` and `-run2` logs in the test worktree. Physical LAN and a full six-human
session remain unverified.

## Acceptance test record (GDD 14.3, T01–T20)

Definitions: [`gdd/14-validation-rubric-map.md`](gdd/14-validation-rubric-map.md#143-priority-acceptance-tests)
and [`gdd/ids.md`](gdd/ids.md). Update a row whenever the test is run (date, build/commit, who, result, evidence).

| ID | Status | Date | Commit | By | Evidence / notes |
|---|---|---|---|---|---|
| T01 | pass | 16/11/2026 | PR SB-42 | Đình Duy | `BalanceProfileTests`: all 9 claims of GDD 14.1 pass; `HazardTelegraphs_LastAtLeastOneSecond` covers H01–H05 (H07 boxes always visible). H06's 1.5 s crack warning gets its assert when its config lands (SB-44). |
| T02 | pass | 16/11/2026 | PR SB-12 | Đình Duy | `CollisionResolverTests.HeadOn_KnownSpeeds_MatchesImpulseFormula` and `CollisionTests.HeadOn_KnownSpeeds_MatchesFormulaWithinTwoPercent`: Normal vs Normal at +5 / −3 m/s → −1.4 / +3.4 m/s, within 2%. |
| T03 | not run | | | | |
| T04 | not run | | | | |
| T05 | not run | | | | |
| T06 | not run | | | | |
| T07 | Một phần | 15/11/2026 | Nhánh SB-16 | Minh Nhật | EditMode kiểm chứng chính sách Survival L2–L5 tại 150 giây. L1 dùng mẫu rơi được cấp trong PlayMode. Chờ chạy đủ năm scene với cả hai luật và H01. |
| T08 | Đạt trong PlayMode | 15/11/2026 | Nhánh SB-16 | Minh Nhật | `SurvivalArenaTests.T08_LastTwoFallOnSameTick_DrawAndRetrySameSeed` đưa hai quả qua kill plane L1 cùng tick. Cả hai hạng 1, chơi lại cùng roster và seed. |
| T09 | not run | | | | |
| T10 | not run | | | | |
| T11 | not run | | | | |
| T12 | pass (PlayMode loopback) | 21/11/2026 | SB-20 recovery, base `39bfc7f` | Codex | Client L1 về MainMenu và hiện thông báo khi host đóng phiên. |
| T13 | not run | | | | |
| T14 | not run | | | | |
| T15 | fail — delayed local response | 21/11/2026 | v0.1.0 / `4f0c0de` | Codex | MPP L3, nominal 100 ms RTT / 2% loss. Client moved on 6/6 instrumented inputs after 344–354 ms. Gust warning matched the rendered host tick and aim. See T15 evidence above. |
| T16 | cut | 16/11/2026 | | Đình Duy | Out of A02 scope: D36 (#82). |
| T17 | cut | 16/11/2026 | | Đình Duy | Out of A02 scope: D37 (#83). |
| T18 | not run | | | | |
| T19 | not run | | | | |
| T20 | not run | | | | |
