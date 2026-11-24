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
## SB-27 — kiểm chứng bổ sung 20/11/2026, chưa nghiệm thu

Base `b856485`, nhánh `feat/SB-27-rank-scoring`. Compiler Roslyn đi kèm Unity 6000.3.28f1
đã biên dịch source các module và source test theo asmdef với reference DLL hiện có ở
checkout chính (chế độ source Editor; assembly AI rỗng dùng một đơn vị compile rỗng).
Đây không phải `tools/verify.ps1`, không import asset và không kiểm tra build Player.

Các method assertion thuần trong ScoreServiceTests, RankMatchTests và SurvivalMatchTests
được gọi bằng runner .NET ngoài Unity tại `../scratch/run_score_tests.py`. Ca đọc
BalanceProfile được gắn `UnityAsset` và không chạy ở runner này. Ca PlayMode
`RankArenaTests.T05_ContactWithoutWobble_CreditsFall_RespawnsAndProtectsFighter` đã viết
nhưng chưa chạy. Kết quả chi tiết và số ca bổ sung được lưu ngoài Assets ở
`../scratch/csc/squishy-bump-sb27/ScoreServiceStandalone.log`.

Kết quả lượt cuối: **28 ca đạt, 0 thất bại** (8 ScoreService, 13 Rank/picker, 7 Survival).
Một ca `UnityAsset` không chạy ở runner bổ sung; đây không phải số pass từ Unity EditMode.

Unity batch thực tế vẫn thoát 198 (`No valid Unity Editor license found`) ở lượt kiểm tra
trên SB-17 trong cùng buổi. `verify.ps1` còn nhận lockfile tồn dư như Editor mở; đã xác nhận
không có Unity.exe rồi gọi batch helper trực tiếp, kết quả vẫn 198. Không xóa cache/lockfile
hoặc hạ gate. SB-27 còn thiếu meta cho bốn script mới; `check_repo` chưa đạt vì các meta đó.
T05 và T07 không đổi trạng thái; cần chạy Unity thật và Windows build trước khi nghiệm thu.

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

## SB-17 — kiểm tra source bổ sung 20/11/2026

Nhánh `feat/SB-17-basic-hud` đã qua compiler Roslyn đi kèm Unity 6000.3.28f1 với DLL
đang có: source module và test biên dịch được; **không chạy test hoặc build Unity**.
Lệnh từ project cha: `python scratch/compile_unity_sources.py squishy-bump-sb17`.
Log bổ sung tại `../scratch/csc/squishy-bump-sb17/`. Unity thực tế còn thiếu license,
batch exit 198; verify nhận lockfile còn sót là Editor mở dù không có Unity.exe.
Chưa import/sinh meta cho MatchHud.cs/MatchHudTests.cs, chưa xác nhận bố cục hình ảnh
hoặc hành vi runtime. Các checklist issue và T-test không đổi từ kiểm tra source này.

## SB-17 — kiểm thử Native 21/11/2026

Nhánh `feat/SB-17-basic-hud`, base `c293fe1`, Unity 6000.3.28f1.
`tools/verify.ps1 -PlayMode` đạt **VERIFY: PASS**, EditMode **165/165**, PlayMode
**38/38**, không lỗi, không warning C#, không test bị bỏ qua. Unity đã sinh meta
cho HUD và test; giữ bản stash WIP trước khi cập nhật main.

- `MatchHudTests`: đồng hồ đỏ ở 10 giây cuối; L1 đổi sang lần rụng vành kế tiếp;
  số quả, tên quả/huy hiệu class và kg, vòng dash 1,5/3 giây và READY, Wobble ×5 đỏ.
- Hồi quy mốc L1 135 giây đã được chạy **đỏ trước khi sửa**:
  `TestResults/HudClockRegression.xml` ghi 1 đạt/1 lỗi vì nhận `Time 15 s` thay
  `Ring 0 s`. Lượt verify cuối xác nhận ca này đã đạt.
- `SyncTests` kiểm tra round-trip cooldown host; snapshot sáu quả là 225 byte.
  Protocol approval tăng thành `LAN-5`; các build khác revision bị từ chối.
- Kết quả XML/log Native: `TestResults/EditMode.*`, `TestResults/PlayMode.*`.
- `tools/build-windows.ps1` đạt **BUILD: PASS**: Windows x64 v0.1.0, 100 MB,
  0 lỗi, 2 phút 33 giây. `python tools/check_repo.py`: PASS (0 problems);
  `git diff --check`: sạch.
- Build tại `Builds/Windows/SquishyBump.exe`. SHA-256 của executable:
  `FBD073CA59C4918EDBC6704F5393CF59021173AF11714C00DB4212C50117492B`;
  `SquishyBump_Data/Managed/SquishyBump.UI.dll`:
  `312D40D9405A9ED4B830014D8B1B852FF18A158FE0ECDD4B917280EB3F0DC46A`.
  Hash assembly phân biệt code HUD; executable Unity có thể giống build khác.
- Chưa quan sát được HUD trong Windows player: công cụ mở ứng dụng không phản hồi
  và hết phiên thao tác. Kiểm tra anchor trong PlayMode chỉ chứng minh vị trí cấu
  hình ở cạnh màn hình, chưa chứng minh khả năng đọc, chồng lấn hoặc các tỉ lệ khác.

Các ca này chưa chứng minh LAN nhiều máy, toàn bộ Rank, hoặc bố cục trên mọi tỉ lệ
màn hình; không nâng trạng thái T07/T15 từ bằng chứng HUD.

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

## SB-25 lobby foundation (#23)

21/11/2026 · `feat/SB-25-lobby-foundation`, base `c293fe1`.
`LobbyRoomTests` checks exclusive fruit claims, the six-player cap, host-only
settings, a five-second countdown at 60 Hz, cancellation when readiness changes,
and the six pick volumes in the greybox scene. `tools/verify.ps1 -PlayMode` passed
169 EditMode and 36 PlayMode tests with zero compile warnings. Networked lobby
movement, live boards, and transition into a match are pending.

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
| T04 | Một phần (luồng tự động) | 24/11/2026 | SB-26, nền `1dd3328` | Codex | `SinglePlayerRunTests`: scene thật L1–L5, bot chạy, retry thua/hòa giữ ID/nhân vật/tier/seed, summary5/5 và retry2; Windows Player4/4. Fixture điều khiển outcome qua kill plane, chưa phải lượt chơi Survival hoàn chỉnh do người thật điều khiển chống bot. [Báo cáo và bằng chứng](testing/sb26-single-run.md). |
| T05 | pass (PlayMode tự động) | 22/11/2026 | PR #101, gameplay `c1824dd` | Codex | `RankRunTests` 8/8. Chạy riêng từng level, số lần rơi/hồi sinh/điểm: L1 18/12/7, L2 15/10/6, L3 17/12/13, L4 17/12/3, L5 17/11/4. Cả năm đạt mục 1–4, 6a (hạng trận), 6b (hạng phiên) và 7, kết thúc trước mốc đóng hồi sinh. Ca 2 quả kiểm mục 5: L1 rơi tick 6301 sau mốc 6300, L2 rơi tick 9000 cùng tick hết giờ. Cả hai ca xác nhận hồi sinh trước mốc, loại cả quả còn mạng sau khi cửa đóng và đồng hạng trận khi rơi cùng tick. Một host đi L1→L5 qua `LevelLauncher.LoadLevel`, đạt 6a mỗi màn và 6b sau L5, tổng 84/56/35 trong lượt verify. Fixture gọi loader giữa các trận. Đồng hạng phiên và xếp hạng nhiều quả sống khi hết giờ được kiểm bằng EditMode. L3/L5 chưa có lượt rơi do chạm đã hết hạn. Bằng chứng `TestResults/T05.xml`, `T05-L*.log`, `T05-Session*.log`, `T05-Close-L*.log`. Verify 223 EditMode, 54 PlayMode, 0 cảnh báo C#. Check repo PASS. |
| T06 | not run | | | | |
| T07 | Một phần | 15/11/2026 | Nhánh SB-16 | Minh Nhật | EditMode kiểm chứng chính sách Survival L2–L5 tại 150 giây. L1 dùng mẫu rơi được cấp trong PlayMode. Chờ chạy đủ năm scene với cả hai luật và H01. |
| T08 | Đạt trong PlayMode | 15/11/2026 | Nhánh SB-16 | Minh Nhật | `SurvivalArenaTests.T08_LastTwoFallOnSameTick_DrawAndRetrySameSeed` đưa hai quả qua kill plane L1 cùng tick. Cả hai hạng 1, chơi lại cùng roster và seed. |
| T09 | not run | | | | |
| T10 | pass (Native PlayMode, 120 s mô phỏng) | 21/11/2026 | SB-44 / base `c293fe1` | Codex | 61 collider thật; kiểm tra từng tick ≥37 và BFS; xem bằng chứng bên dưới. |
| T11 | Một phần — Survival loopback + Rank EditMode đạt | 24/11/2026 | SB-30 trên `main` | Codex / Duy | Peer gửi input rồi rớt; loại ở tick host, motor dừng, trận tiếp tục, lobby mở lại admission; Rank giữ điểm/không respawn trong EditMode; LAN riêng máy chưa chạy. |
| T12 | pass (PlayMode loopback) | 21/11/2026 | SB-20 recovery, base `39bfc7f` | Codex | Client L1 về MainMenu và hiện thông báo khi host đóng phiên. |
| T13 | not run | | | | |
| T14 | not run | | | | |
| T15 | fail — delayed local response | 21/11/2026 | v0.1.0 / `4f0c0de` | Codex | MPP L3, nominal 100 ms RTT / 2% loss. Client moved on 6/6 instrumented inputs after 344–354 ms. Gust warning matched the rendered host tick and aim. See T15 evidence above. |
| T16 | cut | 16/11/2026 | | Đình Duy | Out of A02 scope: D36 (#82). |
| T17 | cut | 16/11/2026 | | Đình Duy | Out of A02 scope: D37 (#83). |
| T18 | not run | | | | |
| T19 | not run | | | | |
| T20 | not run | | | | |

### SB-44 — L4/H06 Native, 21/11/2026

Nền `c293fe1`, nhánh `feat/SB-44-frost-ring`, Unity 6000.3.28f1. Editor entry
`SquishyBump.Editor.IceSceneBuilder.BuildL4` đã tạo scene L4 thật, IceProfile và meta
(exit 0, `TestResults/SB44-BuildL4.log`). Lượt đầu verifier lỗi license exit 198;
sau khi Hub/Licensing Client hoạt động, Editor tự xử lý lockfile và kiểm thử Native chạy được.

- `tools/verify.ps1 -PlayMode` → **VERIFY: PASS**: **179 EditMode + 39 PlayMode**,
  0 lỗi, 0 skip, 0 cảnh báo compile từ code dự án. XML/log ở `TestResults/EditMode.*`,
  `TestResults/PlayMode.*` trong worktree SB-44; tệp sinh không đưa vào Git.
- T10 `IceArenaTests.T10_L4WithoutFruits120SimulatedSeconds_PreservesRealFloor`:
  scene L4 thật, không quả, seed 18, gọi đủ **7.200 tick × 1/60 s**; kiểm tra từng tick
  61 MeshCollider rộng 2 m, μ=0.3, collider bật đúng Solid/Cracking, ≥37 ô đứng được,
  BFS không tách vùng. Đây là 120 giây thời gian gameplay mô phỏng, không phải 120 giây quan sát hình ảnh.
  `IceFieldTests` mở rộng 8 seed × 7.200 tick, gồm cả dự trữ toàn nhóm nứt; kiểm tra
  mốc 30/60 s, trọng số 1+1.5×occupants và chu kỳ nứt/rơi/fade.
- `ReturningTile_RealFighterColliderDelaysSolidUntilClear`: SphereCollider layer Fighter
  thật chồng lên ô đã rơi; sau fade vẫn không bật collider, chuyển quả đi mới hồi Solid;
  raycast sau hồi chạm đúng tile. Không dùng callback overlap giả để chứng minh ca này.
- `HostEventsThroughCodec_ClientFloorMatchesEveryTransitionAndWaitsForSolidEvent`:
  host và replica 61 ô, từng event đi qua SyncCodec; collider khớp mỗi tick trong 120 giây.
  Replica vẫn không bật sàn khi chỉ biết Warning mà chưa nhận Solid. Ca này không tạo
  NGO peer hoặc mô phỏng trễ mạng, không chứng minh LAN nhiều máy.
- Hồi quy đã thấy đỏ trước sửa: Native đầu **36 đạt / 3 lỗi** do tạo
  MaterialPropertyBlock trong field initializer khi Unity deserialize IceTiles
  (`TestResults/SB44-BeforeAwakeFix.xml/log`). Chuyển tạo vào Awake; ba ca trên
  **3/3 đạt**, sau đó toàn bộ **179/39 đạt**. `TestResults/SB44-IceFix.xml/log`
  lưu lượt kiểm tra riêng sau sửa.

Chưa kiểm chứng: quan sát player Windows (màu/material, crack/shake/fade, đọc cảnh báo),
đo trượt thực tế cùng hạng cân, LAN trên PC riêng, AI-2/LD06, T07 đủ năm level/cả hai luật.
Không thay đổi trạng thái các test đó bằng bằng chứng T10.

Build Windows x64 v0.1.0 qua `tools/build-windows.ps1` đạt, Unity trả exit 0;
`Logs/build-windows.log` lưu BuildScript report. Build không chứng minh hình ảnh đã được quan sát.

#### SB-44 bổ sung — replay qua hai chu kỳ khi client render trễ

`ClientRenderingBeforeRestore_QueuedNextWarningKeepsOldTileUnwalkable` đã chạy Native
**0 đạt / 1 lỗi** trước sửa (`TestResults/SB44-ClientHistoryBeforeFix.xml/log`):
client nhận Solid tại 18 s và Warning cho lần rơi 19.5 s, nhưng render 17.9 s lại bật
collider trong khi ô vẫn đang Fading. Đây là tình huống ô chờ hết overlap rồi bị chọn
lại ngay, client nội suy sau host. Sửa IceTiles giữ lịch sử chuyển trạng thái của
một match; warning mới không xóa trạng thái chu kỳ cũ. Ca hồi quy còn kiểm tra các
mốc 18/19.5 s và rewind về 17.9 s.

Sau sửa: verifier Native **179 EditMode + 40 PlayMode đạt**, 0 lỗi/skip/cảnh báo
compile từ code dự án; ca mới đạt 0.327111 s. Build lại Windows x64 **100 MB,
0 errors**, 25.63 s. Repo/diff checks đạt. Wire format và revision LAN-7 không đổi.
Đây là kiểm chứng logic replay/collider, chưa phải LAN nhiều PC hoặc quan sát hình ảnh.

### SB-45 — L5 Chaos Core Native, 21/11/2026

Nhánh `feat/SB-45-chaos-core`, base `85b3c9c` của PR #102 (SB-44), Unity
6000.3.28f1. `IceSceneBuilder.BuildL5` tạo scene qua Editor Native, exit 0;
log `TestResults/SB45-BuildL5.log`, meta test do Unity sinh. Chỉ sửa L5 và đường
Editor dùng chung; scene L2/L3 và thuật toán Cannon/H06 giữ từ nhánh phụ thuộc.

`tools/verify.ps1 -PlayMode` → **VERIFY: PASS**, **179 EditMode + 41 PlayMode**,
0 lỗi, 0 skip, 0 cảnh báo compile từ code dự án. Hai ca L5 trong
`TestResults/PlayMode.xml`:

- `L5_RealSceneReuses61IceTilesAndWestAirEastFire_AllSpawnsHaveFloor`: nạp L5 thật,
  61 collider ô, một pháo Air ở tây x=-9.9 và một Fire ở đông x=+9.9; không có
  MovingGround/RingDrop; raycast cả sáu spawn bán kính 6 m xuống đúng sàn H06,
  ma sát 0.3. Đây là kiểm tra hình học/collider, không phải đánh giá camera hoặc hình ảnh.
- `L5_150Seconds_IceWavesEvery210TicksAndCannonsEvery240Ticks`: gọi cả H06 và Cannons
  trên host 9.000 tick × 1/60 s, seed 18, không quả. Ghi nhận **42 đợt nứt** đúng
  mỗi 210 tick, 2 ô trước mốc 45 s và 3 ô sau đó, cảnh báo nứt 90 tick. **37 phát
  mỗi pháo**, mỗi cặp phát liên tiếp cách 240 tick, tất cả có CannonAim trước
  60 tick. Mỗi tick kiểm tra ≥37 ô Solid dự trữ, BFS liên thông và collider đúng
  trạng thái. Giữ stagger 1.25 s của Cannons hiện có (Air đầu 4 s, Fire đầu 5.25 s).

Các ca này chạy 150 giây thời gian gameplay mô phỏng, không phải quan sát 150 giây
hình ảnh hoặc trận đủ sáu quả. Chưa kiểm chứng: cảnh báo chồng nhau có dễ đọc,
đường né thực tế/AI, Windows player và LAN nhiều PC. Không dùng bằng chứng này
để đánh dấu T07, T04/T05 đầy đủ, LD07 hoặc AI-2 đạt. Cần merge PR #102 trước,
sau đó merge main vào nhánh SB-45, chuyển base PR về main và xác minh lại.

Lượt cuối sau khi đưa bản sửa replay SB-44 `c918aa6` vào nền: **179 EditMode +
42 PlayMode đạt**, 0 lỗi/skip/cảnh báo compile; build lại Windows x64 v0.1.0
**100 MB, 0 errors, 22.14 s**. Verifier và build đều exit 0. Ca hồi quy client
render trước tick hồi cũng chạy trong bộ 42 ca. Repo/diff checks đạt. Base PR L5
là `feat/SB-44-frost-ring` tại c918aa6; chỉ delta SB-45 so với nhánh đó được commit.
Stash trước tích hợp c918aa6 được giữ, không xóa backup WIP.

# SB-24 — kiểm tra bổ sung 21/11/2026

Nhánh `feat/SB-24-match-events`, base `c293fe1`: Roslyn đi kèm Editor 6000.3.28f1
compile source các module, Editor và test. Runner .NET ngoài Unity gọi assertion
`MatchEventTests` + `SurvivalMatchTests`: **18 ca đạt, 0 lỗi**. Phạm vi: sáu loại
event qua codec, payload sai/truncated/extra, bảng điểm và tie, dữ liệu bất biến,
luồng MatchDirector → codec → hai replica, ID trùng/cũ và Survival regression.
Không phải NUnit Test Runner của Unity, không phải T06/T11/T12/T18 đạt.

`MatchEventNetworkTests.ReliableEvents_LoopbackReplicasAndResultTextAgreeAndDuplicateIsIgnored`
đã viết nhưng chưa chạy: tạo hai NGO peer loopback, gửi đủ sáu loại event, truyền lại
result cùng ID, kiểm tra đúng một lần và nội dung kết quả giống nhau. Còn phải chạy
scene-load barrier với LAN thật, đồng bộ Rank sau tích hợp SB-27, các sàn và hai luật.

`tools/verify.ps1 -PlayMode` dừng Unity exit **198**, không sinh XML; log EditMode/PlayMode
ghi `No valid Unity Editor license found`. Dòng `Compile: OK` của wrapper không chứng
minh Unity đã import/compile. Repo gate còn thiếu ba `.meta` do Editor chưa sinh.
Không viết GUID tay, chưa commit/push/PR, không đánh dấu tiêu chí issue #22.

## SB-24 — bằng chứng Native sau khi khôi phục license 21/11/2026

Unity 6000.3.28f1 đã import nhánh `feat/SB-24-match-events` (base `c293fe1`), sinh đủ
ba meta. `tools/verify.ps1 -PlayMode`: **VERIFY: PASS**, **176 EditMode/37 PlayMode**,
0 failed, 0 skipped, 0 warning C#. XML/log tại `TestResults/EditMode.*`,
`TestResults/PlayMode.*`; lượt import đầu tại `TestResults/MatchEventInitialImport.*`.

- `MatchEventTests`: round-trip sáu loại, payload lỗi, bất biến standings, shared
  rank và Director Survival → codec → replica, không áp lại ID trùng/cũ.
- `MatchEventNetworkTests.ReliableEvents_LoopbackReplicasAndResultTextAgreeAndDuplicateIsIgnored`
  đạt trong **0,321320 s**: hai NGO peer qua 127.0.0.1:7777, scene management tắt;
  host phát countdown, ring-out, respawn, elimination, scoreboard, result; phát lại
  result cùng ID. Client nhận đúng sáu event, replica và chữ kết quả khớp host.
  Đây là dữ liệu event được fixture cấp, không phải một match Rank đã chơi.
- `python tools/check_repo.py`: PASS (0 problems); `git diff --check`: sạch.

Build của lượt Native SB-24: `tools/build-windows.ps1` đạt **BUILD: PASS**, Windows
x64 v0.1.0, 100 MB, 0 lỗi, 2 phút 26 giây. Thư mục `Builds/Windows/`; SHA-256 của
`SquishyBump_Data/Managed/SquishyBump.Net.dll`:
`CA93A3602457B5EE0B8F9F15C2404C98043D9B4EFA916ADEB4EB0E0D1BD6F83D`.

Chưa kiểm tra scene-load barrier với nhiều PC, kết quả trên LAN vật lý, hoặc event
Rank từ ScoreService của SB-27. Chưa nâng T06/T11/T18 hoặc nghiệm thu toàn bộ hai luật.

## SB-27 — Native Rank nối PR #99, 21/11/2026

Nhánh `feat/SB-27-rank-scoring`, base `43eb004` của PR #99; WIP cũ giữ trong stash
`7190f3a012144da1a2a0508649814836b8557278`. Đã đối chiếu GDD VI v1.3 trang 9,
mục 3.6: tổng điểm → tổng mạng còn → tổng thời gian sống cho session; match Rank
hết giờ xét mạng rồi Wobble. Meta bốn script mới do Unity sinh.

`tools/verify.ps1 -PlayMode` cuối: **VERIFY: PASS**, **199 EditMode/38 PlayMode**,
0 failed/skipped, 0 warning C#. `python tools/check_repo.py`: PASS (0 problems).
XML/log tại `TestResults/EditMode.*`, `TestResults/PlayMode.*`.

- Chính sách Native kiểm tra 3 mạng, 120 tick hồi sinh, 90 tick bảo vệ, Wobble về 0,
  chạm cuối trong 180 tick (gồm đúng biên), hazard/tự chạm không cho điểm; cửa sổ L1
  đóng tại 105 s, L2–L5 tại 150 s; kết quả/tie và tổng session không cộng lại lần hai.
- Regression `SoleEligibleFruitWaitingToRespawn_RemainsTheWinnerInTheReplicatedResult`
  đã đỏ trong `RankPendingWinnerBeforeFix.xml` (198 đạt/1 lỗi): result bỏ quả còn mạng
  đang chờ hồi sinh khiến codec từ chối payload. Sau sửa, cả quả đó và quả bị loại đều
  có đúng ID/rank/mạng trong result round-trip, không còn hàng chờ hồi sinh sau chốt.
- PlayMode đã bắt lỗi đặt Rigidbody khi object tắt không cập nhật pose dùng lúc bật
  lại: `RankRespawnDiagnostic.xml` ghi body/transform vẫn xấp xỉ y=-6. Sửa
  PlayerMotor.ResetForRespawn cập nhật cả Transform và Rigidbody, giữ state/dash/stun reset.
  `RankRespawnFix.xml` đạt sau sửa (2,720783 s); suite cuối cũng đạt.
- `RankArenaTests.T05_Partial_ContactWithoutWobble_CreditsFall_RespawnsAndProtectsFighter`
  chạy physics trên scene L1 với ba fighter, timeScale 4: tiếp xúc dưới ngưỡng Wobble
  vẫn cho +1 khi rơi, mất một mạng, ẩn khi chờ rồi về vị trí trên sàn, Wobble/dash/stun
  reset; impulse/Wobble bị chặn khi bảo vệ, nhận impulse lại khi hết bảo vệ và không rơi lặp.
- Producer Rank đã nối Scoreboard/Respawn/Elimination/Result vào EventHub của PR #99.
  Bảng gồm cả quả chờ, mang points/lives/survivalTicks; protocol nhánh là LAN-6.

Đây **chưa phải T05 đầy đủ**: chưa chạy Single Rank L1–L5 với progression/roster bot,
chưa có LAN nhiều PC hoặc kiểm tra hình ảnh blink. T05 giữ "Một phần", T07 không nâng.
Không dùng case physics L1 hay test domain để ghi nghiệm thu toàn session.

`tools/build-windows.ps1` đạt **BUILD: PASS**, Windows x64 v0.1.0, 100 MB, 0 lỗi,
2 phút 29 giây. Build tại `Builds/Windows/SquishyBump.exe`; SHA-256 assembly trong
`SquishyBump_Data/Managed/`: Match.dll
`F299F04CD55473524F801920BFEF173BE774FEDCDAED9253E260461BFA148696`, Physics.dll
`A71CB7F2D461F44EF1F9D9C0EF12969668B312553C69D7BD0FA5B7ACB7DD5FC5`.

Nền PR #98 (BotBrain D1, 53966b7) cùng PR #103 (L4/L5, 2e87aa8), Unity
6000.3.28f1: tools/verify.ps1 -PlayMode đạt **185 EditMode + 43 PlayMode**,
0 lỗi/skip/cảnh báo compile; repo check đạt. Xung đột asmdef đã giữ cả AI và uGUI,
không lặp tham chiếu. Đây chỉ là kiểm tra nền phụ thuộc; chưa chứng minh các tiêu
chí danger map, A* hoặc utility của SB-46. Chưa có build Windows cho nền ghép này.

### SB-46 — navigation/utility Native, 22/11/2026

Nhánh feat/SB-46-danger-navigation, nền tích hợp dd89fbd (PR #98 BotBrain và PR #103
L4/L5); main vẫn c293fe1 khi fetch lại. Unity 6000.3.28f1. Bộ cuối
`tools/verify.ps1 -PlayMode` → **VERIFY: PASS: 204 EditMode + 47 PlayMode**,
0 lỗi/skip/cảnh báo compile từ code dự án. `tools/build-windows.ps1` đạt Windows x64
v0.1.0, **101 MB, 0 errors, 25.25 s**. XML/log ở TestResults/EditMode.*, PlayMode.*;
build log ở Logs/build-windows.log trong worktree SB-46, không đưa thư mục sinh vào Git.

- NavigationTests: giới hạn 1 m/26×26, max edge/telegraph, awareness, margin băng,
  vùng circle/annulus/box/cone/hex có xoay, ô rơi bị chặn, chặn cắt góc, đường né vùng
  nguy hiểm và replan khi sàn đổi. **50 lưới ngẫu nhiên seed 0–49** đối chiếu A* với
  oracle Dijkstra O(V²) độc lập, gồm chi phí và trường hợp không có đường.
- Công thức utility kiểm tra riêng từng hạng cân; ngưỡng U≥0.4 và hysteresis 0.15.
  Chưa có đánh giá tỷ lệ thắng hoặc cân bằng người/bot từ các ca toán học này.
- NavigationArenaTests: L4 thật trước/sau 2 ô nứt rồi rơi, trước còn đường/sau bị
  chặn; đặt collider Fighter thật trên lỗ vẫn không bị nhận nhầm làm sàn. L3 chỉ lộ
  marker sau lúc phóng đạn, nhận cone khí và patch từ trạng thái thật. L2 chạy thời
  gian tiến 30 s, lấy cảnh báo đổi chiều ở 13 s, kiểm tra hộp/làn pusher và ngân sách
  lưới. Những ca gọi Step trực tiếp kiểm tra dữ liệu cảm nhận, không phải playtest
  bot né hazard hoặc kiểm tra chất lượng hiển thị.
- BotArenaTests: năm D1 trong match L1 dùng navigation/replan và đều di chuyển
  >0.15 m qua PlayerMotor/Rigidbody thật trong 2 s; fall đổi Eliminated. Ca D2 nạp
  profile D2 trước StartMatch, năm bot thật chọn mục tiêu utility/lập đường và có
  dash tới PlayerMotor trong cửa sổ 6 s gameplay. D2 chạy Time.timeScale=4 để rút
  thời gian chờ; physics vẫn fixed 1/60 s. Đây không phải T04/T05 hoặc AI-1–AI-6.

Ba hồi quy đã thấy đỏ trước sửa, giữ XML/log riêng:

1. SB46-UtilityBoundaryBeforeFix: **201 đạt/1 lỗi**, 0.75f−0.60f bị coi nhỏ hơn 0.15f.
   So sánh biên có xét sai số float; không hạ ngưỡng thiết kế.
2. SB46-ApproachFacingBeforeFix: **202 đạt/1 lỗi**; bot tới điểm tiếp cận nhưng nhìn
   waypoint nên không charge. Tách yaw nhìn mục tiêu khỏi vector di chuyển theo path.
   Fixture tích phân InputCommand là kiểm tra ý định, không được tính là PlayerMotor;
   ca D2 Native ở trên kiểm chứng riêng luồng motor thật.
3. SB46-EvadeReachabilityBeforeFix: **203 đạt/1 lỗi**; chọn ô gần bị ngăn bởi khe rồi
   đứng lại. Chuyển sang tìm ô an toàn tới được có cost thấp nhất trong 4 m.

Các ca trên đều xanh trong bộ cuối 204/47. Repo/diff checks phải sạch trước commit.
Chưa kiểm chứng: quan sát path/debug trong player, hiệu năng năm D5, AI-1–AI-6,
trượt/né hazard của các bậc trên đủ năm sàn, T04/T05/T07 đầy đủ hoặc LAN nhiều máy.
Không đổi trạng thái các test đó dựa vào bằng chứng này. PR phụ thuộc #98 và #103;
cần tích hợp các PR phụ thuộc trước, merge main vào nhánh này, chuyển base về main
và xác minh lại; không tự merge PR trong đợt làm việc này.


### SB-17 — sửa review PR #97, 23/11/2026

Nền a7b1656 đã tích hợp main vào feat/SB-17-basic-hud. Unity 6000.3.28f1 Native:
`tools/verify.ps1 -PlayMode` → VERIFY: PASS, **242 EditMode + 60 PlayMode**,
0 lỗi/cảnh báo compile; 1 skip có chủ đích là HudPlayerVisualTests chỉ chạy Windows Player.
Test đó chạy riêng trong StandaloneWindows64 với Direct3D12: **1 đạt, 0 lỗi**,
8 cảnh ở 1920×1080, 1280×720, 1440×900, 1680×720; đủ 6 tag, không giao chữ/quả.
Đã xem trực tiếp cả 8 ảnh. Ba hồi quy đã chạy đỏ trước sửa: nhãn che chính quả,
nhãn chồng nhau và nhãn che quả khác. [Ảnh, JSON, XML và lệnh chạy lại](testing/sb17-hud-review/README.md).

Giữ anchor cố định, sửa Wobble lấy đỉnh model chiếu lên màn hình và tránh giao nhau.
Fixture L1 freeze/đặt pose để đo, không thay thế chơi tay/LAN/T07; không nâng trạng
thái các test đó. PR tiếp tục draft chờ cornel05 review theo yêu cầu.

Release Windows x64 trên nền này: `tools/build-windows.ps1` → **BUILD: PASS**,
101 MB, 0 errors, 48,85 s. `python tools/check_repo.py` → PASS (0 problems).
# SB-25 nối tiếp PR #95 — tiến độ 21/11/2026, chưa nghiệm thu lobby

Nhánh `feat/SB-25-3d-lobby` fast-forward lên `ea46852` (PR #95 đang mở) để dùng scene
và LobbyRoom đã có. Bằng chứng 169 EditMode/36 PlayMode của PR #95 là lượt đồng đội
ghi, không phải kết quả của thay đổi nối tiếp này.

Source mới: Core LobbyState bất biến/ILobbyChannel; LobbyCodec tối đa 73 byte cho
6 người, kiểm tra version/length/duplicate ID/fruit/ready; LobbySync reliable NGO,
chỉ nhận state từ host và chỉ nhận ready của sender thật. LobbyCoordinator nối
membership, clock, lựa chọn từ adapter host và countdown; không broadcast state
mỗi tick khi idle, countdown cập nhật từng giây. Bootstrap poll transport và lưu
lựa chọn qua đổi scene. LobbyController nối motor/input/snapshot cho host/client,
kiểm tra pick volume theo vị trí host; LobbyPanel đặt board world-space và quầy host.
LevelLauncher đưa Single/Host qua Lobby, Single vào L1, LAN roster chỉ có người thật
theo fruit đã chọn. Rule/Lk được giữ trong LobbySelection; luồng các trận tiếp theo
còn phải tích hợp SB-26/SB-29. Không có đường Start match bỏ qua ready từ menu cũ.

Roslyn compile toàn bộ source module/Editor/test đạt; 10 ca assertion thuần ngoài
Unity đạt: codec 1/2/6 người, payload sai, bất biến, ready/countdown/leave, coordinator
host/client và cleanup. Chưa phải Unity test hoặc bằng chứng lobby đi lại được.
Đã bổ sung LobbyPlayTests cho hai peer loopback: chọn bằng vị trí trong volume,
từ chối trái đã có chủ, ready qua NGO, countdown rồi roster hai người đúng trái.
MenuFlowTests đổi theo menu → lobby → chọn trái → L1. Các ca Native này chưa chạy.

`tools/verify.ps1 -PlayMode` FAIL: Unity EditMode và PlayMode exit198 do license,
không có XML. Lệnh ProjectScaffold.Run mới (`TestResults/SB25LiveScaffold.log`) cũng
exit198; LobbyProfile.asset/gán MatchSetup chưa sinh. Repo check thiếu 9 meta mới.
Diff sạch. Chưa commit/push/PR nối tiếp; các tiêu chí issue #23 giữ mở. Cần Editor
import/scaffold, verify/build và kiểm tra trực quan đi lại/UI/input/shelf locks,
LAN riêng máy và scene-load barrier của SB-24. Không cắt thành lobby 2D.
Lượt verify sau runtime chọn live Editor vì lockfile còn sau startup lỗi rồi dừng
do không tìm thấy CLI `unity`; vẫn không có XML. Không sửa cache/lock để vượt gate.

## SB-25 — Native lobby 3D, 21/11/2026

Nhánh `feat/SB-25-3d-lobby`, base `ea46852` của PR #95. Unity 6000.3.28f1 đã chạy
`ProjectScaffold.ConfigureLobbyDefaults`: tạo LobbyProfile, gán walking class và
MatchSetup bằng Editor API, sinh đủ meta. Giữ greybox 3D, không chọn phương án cắt 2D.

`tools/verify.ps1 -PlayMode` cuối đạt **VERIFY: PASS**, **179 EditMode/37 PlayMode**,
0 failed/skipped, 0 warning C#. `python tools/check_repo.py`: PASS (0 problems);
`git diff --check`: sạch. XML/log tại `TestResults/EditMode.*`, `TestResults/PlayMode.*`.

- Native đã bắt lỗi: so `FastBufferReader.Length` khiến LobbySync loại gói hợp lệ
  vì NGO đã đọc header tên message. `LobbyDiagnostic.xml` ghi timeout tại host countdown;
  sửa dùng số byte còn lại (`Length - Position`). Lỗi tham chiếu khi owner bị hủy trong
  teardown cũng đã được tái hiện và sửa. Lượt đỏ đầu giữ trong `LobbyFailureBeforeFix.*`.
- `LobbyWireFix.xml` đạt sau sửa, rồi ca cuối
  `LanLobby_HostVolumesEnforceExclusiveClaims_ReadyStartsTwoHumanRoster` đạt **4,023434 s**:
  host vào scene Lobby; client NGO loopback nhận info; gửi InputCommand đi phải rồi
  dừng, avatar host dịch ngang >0,15 m qua PlayerMotor; chọn bằng vị trí trong volume,
  từ chối kệ đã có chủ và nhãn kệ hiện "ĐÃ CHỌN"; đổi rule/Lk chỉ khi host gần quầy;
  info chứa trái/ready/IP; ready qua NGO, countdown rồi L1 với đúng hai ID/trái đã chọn.
  Sau khi kiểm tra countdown đang mở, fixture dùng timeScale 4 để rút ngắn thời gian
  chờ; policy Native kiểm tra đủ 300 tick = 5 giây ở 60 Hz. Không có gói hợp lệ bị loại.
- `MenuFlowTests` đạt menu → lobby → chọn trái → L1 → pause → leave. Codec/coordinator
  kiểm tra state 1/2/6 người, dữ liệu sai, quyền host, stale revision, leave/cancel và
  countdown chỉ mở khi tất cả ready.

Chưa kiểm tra trực quan glyph/layout từ camera trong Windows player, LAN các PC riêng
hoặc scene-load barrier tích hợp SB-24. Tiến trình session nhiều màn/D1 bots thuộc
SB-26/SB-29/SB-18 khi tích hợp. Không nâng T06/T07 hoặc bài LAN nhiều máy từ loopback.

`tools/build-windows.ps1` đạt **BUILD: PASS**, Windows x64 v0.1.0, **101 MB**, 0 lỗi,
2 phút 25 giây. Build tại `Builds/Windows/SquishyBump.exe`. SHA-256 assembly trong
`SquishyBump_Data/Managed/`: App.dll
`C033BC93CEDF1A179D7F341EE30D46244CE1C57D6C0E10432C3B97A3A8372F88`, UI.dll
`B939A2A79D66B37A6DA6FA008D91D1C51551DCBC8DE2D09C5EB79C915EDB7F6B`.


SB-17 kiểm chứng cuối sau tích hợp main **c534179** (lobby PR #100), protocol
**LAN-10** tránh trùng LAN-9 lobby: `tools/verify.ps1 -PlayMode` → **VERIFY: PASS,
252 EditMode + 61 PlayMode**, 0 lỗi/cảnh báo compile. Một skip Windows-only có
chủ đích; chạy riêng Windows Player Direct3D12 đạt **1/1**, cả 8 ảnh/JSON mới đã xem.
`tools/build-windows.ps1` → **BUILD: PASS, 101 MB, 0 errors, 51,49 s**.
[XML cuối và manifest](testing/sb17-hud-review/manifest.json). Repo check và diff sạch.
Không nâng T05/T07/LAN từ fixture hình ảnh này. Giữ draft chờ cornel05 review.

## 23/11/2026 · SB-29 Survival LAN, T18

- Tái hiện trước sửa (`TestResults/SB29-red.xml`): client nhận kết quả L1, host không có nút **Tiếp tục**.
- PlayMode sau sửa (`TestResults/PlayMode.xml`): hai peer qua L1 → L5, giữ nguyên ID/trái và nhận cùng kết quả phiên. Ca ba người loại host ở L1, host spectate L2 và client còn lại nhận kết quả khi đối thủ rời. Ca hòa ở màn cuối tải lại cùng màn, roster và seed rồi chốt một vô địch.
- EditMode kiểm công thức với 6 người/5 màn và 4 người/3 màn, phòng 2 người, hòa cắt ngưỡng và bỏ cuộc. Phần PlayMode dùng NGO loopback cùng tiến trình, tắt NGO scene management và nạp scene ở host. T18 nhiều người trên các máy riêng còn chưa kiểm chứng.
- `tools/verify.ps1 -PlayMode` → PASS, 260 EditMode và 62 PlayMode, 0 failed/skipped, 0 cảnh báo C#. `python tools/check_repo.py` → PASS.
- Windows x64 → BUILD PASS, 101 MB, 0 lỗi. Hai tiến trình build mới Host/Join `127.0.0.1` vào cùng lobby. Chuyển L1 → L2 trên hai cửa sổ còn cần kiểm thủ công vì điều khiển tự động bị timeout khi đi tới kệ trái.

## 23/11/2026 · SB-75 UI kit "Assets cho UX/UI"

- `python tools/ui/import_uxui.py <thư mục Drive>` → 29 sprite (PNG 1x), giữ GUID của các nút cũ. Không nhập ảnh `[Demo]`.
- Ảnh chụp Play Mode 1920×1080 so với `[Demo]` của từng màn: [Start](testing/sb75-ui/start.png), [Settings](testing/sb75-ui/settings.png), [Pause](testing/sb75-ui/pause.png), [HUD](testing/sb75-ui/hud.png), [Round](testing/sb75-ui/round.png), [WINNER](testing/sb75-ui/winner.png). Canvas co theo cả hai trục (match 0.5), nên cửa sổ rộng hơn 16:9 không đẩy hàng kết quả đè lên nút Next.
- Test mới: `UiKitTests` (sprite, viền 9-slice, kích thước console), `MatchHudTests.Board_SortsByValueAndKeepsOrderOnTies`, `MatchOverlayTests` (tim xám khi mất mạng, banner Round n, chỉ hàng của mình không bị phủ xám, bục top 3).
- `tools/verify.ps1 -PlayMode` → PASS, 308 EditMode và 68 PlayMode (1 skip Windows-only có sẵn), 0 cảnh báo C#. `python tools/check_repo.py` → PASS.
- Chưa kiểm: bản build Windows và LAN hai máy thật (WINNER chỉ xem qua fixture).

## SB-76 · 23/11/2026 · Animal characters + claw-machine lobby

- Six animals (Bear, Pig, Cat, Penguin, Chick, Frog) replace the fruits, same slots/classes/GUIDs; `Squishy Bump/Swap To Animals` (`AnimalSwap`) renames assets and fits `Visual/Body/Model` (width 2 × radius, feet at −radius).
- Fighters stay upright: `PresentationProfile.upright` → `RollVisual` turns the body to the velocity (720°/s), leans up to 14° at 6 m/s, keeps squash. Physics untouched.
- Avatars: `Squishy Bump/Render Avatars` writes `avatar_0..5.png` ([avatars](testing/sb76/avatars.png)).
- Lobby: `Squishy Bump/Build Lobby` places `Art/Lobby/Lobby.glb` (×3, turned 180°), pedestals → `Stand n`, prize-chute button → `Host desk`, colliders for floor/walls ([follow view](testing/sb76/lobby_follow.png), [overview](testing/sb76/lobby_high.png)); roster check: [six animals](testing/sb76/roster.png).
- Tests: `FighterVisualTests` (model from `Art/Characters`, width from renderer bounds, single sphere collider), `PresentationTests` (`FacingYaw`, `UprightRotation`), `PresentationPlayTests.Upright_FacesVelocityWithoutRolling`; names updated in other tests.
- `tools/verify.ps1 -PlayMode` → PASS, 310 EditMode and 68 PlayMode (1 skip Windows-only), 0 C# warnings. `python tools/check_repo.py` → PASS.
- Not checked: screenshots are Editor renders (camera pose of the lobby follow view), not Play Mode captures; lobby claim/desk flow is covered by `LobbyPlayTests`, not eyeballed; Windows build size (+34 MB GLB) not measured; Sketchfab licences pending (see CREDITS.md).

## SB-77 · 23/11/2026 · Lobby UI kit + room options

- Kit from Drive folders 7, 8, 9 (`tools/ui/import_lobby_ui.py`): lobby console, Find Server console, row pill, 3 class cards. Credits and asset ledger rows added.
- Lobby host desk is `LobbyConsole`: tabs MATCH SETUP (6 slots, animal avatars) / MAP (final level), D-pad flips Survival/Rank, CAPACITY slider (2-6), LOBBY MODE private/public, PASSWORD, START MATCH. Class cards above the stands take their numbers from `BalanceProfile`.
- Net (host-enforced): `LobbyRoom.TryConfigureRoom`, `StubSessionService` password hash + capacity in connection approval (`WrongPassword`), protocol `SquishyBump-LAN-11`, `LobbyCodec` v2 (capacity, public). LAN beacon `LanDiscovery` (UDP 7778, public rooms only, no password in the packet) feeds the Find Server list.
- Tests: `LobbyRoomTests` (options), `LobbyCodecTests` (capacity/public), `SyncTests` (password, capacity, payload size), `LanDiscoveryTests`, `UiKitTests` (new sprites and sheet sizes), `RoomOptionsPlayTests` (loopback: wrong password refused, capacity, listener), `LobbyPlayTests` driven through the console buttons.
- Play Mode screenshots (`unity command capture game_view`): [Find Server](testing/sb77/find_server.png), [lobby console](testing/sb77/lobby_console.png), [MAP tab](testing/sb77/lobby_console_map.png), [class cards](testing/sb77/lobby_stands_cards.png).
- `tools/verify.ps1 -PlayMode` → PASS, 347 EditMode and 72 PlayMode (1 skip Windows-only), 0 C# warnings. `python tools/check_repo.py` → PASS.
- Not checked: two real PCs on a LAN (discovery used loopback only; Windows Firewall may block UDP broadcast, the manual IP join is the fallback), gamepad on the new screens, the "PRIVITE" typo baked into the team's art.

### SB-26 — 24/11/2026

Nền main `1dd3328`; `tools/verify.ps1 -PlayMode`: 356 EditMode +76 PlayMode đạt,
0 lỗi/cảnh báo C#, 1 skip HudVisual dành cho Windows Player. Bốn ca Single Player
Windows D3D12 đạt riêng. Fixture lobby đã tách hai lần nhấn đổi luật sang hai
frame để đọc snapshot đã render. T04 giữ Một phần như bảng trên;
[xem báo cáo và bằng chứng](testing/sb26-single-run.md).
Build Windows x64 release đạt: 0 errors, 857 MB; repo checker PASS (0 problems).

### SB-30 — đối soát lên `main` (24/11/2026)

- Hợp nhất PR #96 với `main`: dùng chung `StubSessionService.AcceptingPlayers` (`GameplayBootstrap.LockLanRoster` / `UnlockLanRoster`), bỏ `ISessionAdmission`; gom phát `MatchEvent.Elimination` về `MatchDirector` cho cả Survival và Rank (không respawn, không cộng ring-out credit, giữ nguyên điểm đã ghi).
- Kiểm thử: `SurvivalMatchTests` (đã cập nhật sang `GameEventHub`), `RankMatchTests.Disconnect_EliminatesWithoutRespawnOrCreditAndKeepsScore`, `SyncTests`, `MenuFlowTests.T11_ClientLeaves_RemoteInputStopsAndLobbyReopensAdmission` (loopback). Chưa chạy LAN 2 máy riêng.

### SB-78 — gắn Creative map assets L1–L5 & direct launch (24/11/2026)

- Assets & scenes: đưa raw GLBs/meshes/materials vào `Assets/_Project/Art/Levels/L1..L5/`, gắn vào `L1..L5.unity` qua `LevelArtSceneBuilder` (`TrainingRingFloor.prefab` / H01 trên L1, `LevelArtBinder` runtime trên L2/L4/L5). Giữ tỉ lệ visual fighter `1.0x` đợt này (mốc scale đã thử nghiệm trước đó giữ lại trong ghi chú: `L1: 0.95x`, `L2: 0.80x`, `L3: 0.60x`, `L4: 0.90x`, `L5: 0.85x`).
- Inject màn chơi: CLI `-level 1..5` (`L1..L5`) & `-rule survival|rank` (`LevelLauncher` + `GameplayBootstrap`), `LevelPlayCommands.PlayL1..PlayL5`, `tools/play-level.ps1`.
- Tests mới: `LevelLaunchArgsTests` (EditMode), `LevelArtTests` (PlayMode L1–L5).
- Kết quả sau rebase lên `main` (`bcc115a`): `tools/verify.ps1 -PlayMode` → **VERIFY: PASS** (**376** EditMode, **82** PlayMode passed, **1** skipped WindowsPlayer-only, **0** cảnh báo `SquishyBump.*`); `python tools/check_repo.py` → **PASS** (0 problems); `tools/build-windows.ps1` → **BUILD: PASS** (**0 errors**, **3993 MB**). Chưa playtest tương tác trực tiếp trong `.exe` L1–L5 qua `play-level.ps1`.

### SB-79 — đồng bộ Creative map assets L1 & L2 từ Drive (25/11/2026)

- L1: trích `sand01_Normal.jpg` (`648 KB`) và màu `Sand` vàng cát linear `(0.80, 0.565, 0.125)` từ `cat vong 1..4.glb` sang `Band.mat` (giữ `Fixed_Vong1..4.asset` đúng bán kính đồng tâm & UV/tangents, tránh normal đảo/trùng bán kính của `cat vong 2..4`); nâng `Map1Demo.glb` → `Map1.glb` (ẩn `Torus.001..006` tĩnh) và thay bộ phao mới (`PhaoTron1..3`, `PhaoUnicorn1..2`, `PhaoLgbt`).
- L2: thêm `chuotlatdat.glb` (`15.7 MB`) và `meolatdat.glb` (`12.1 MB`) gắn lên 3 chướng ngại vật xoay `Box 0..2` của `MovingGround` qua `LevelArtBinder` (giữ nguyên `BoxCollider`). L3–L5 đã khớp 100% với Drive nên giữ nguyên.
- Kết quả: `tools/verify.ps1 -PlayMode` → **VERIFY: PASS** (**376** EditMode, **89** PlayMode passed, **2** skipped, **0** cảnh báo `SquishyBump.*`); `python tools/check_repo.py` → **PASS** (0 problems); `tools/build-windows.ps1` → **BUILD: PASS** (**0 errors**, **3847 MB**). Chưa mở `.exe` playtest tương tác trực tiếp L1/L2.

### SB-80 — hoàn thiện Map 1: texture ván gỗ & dọn mesh dư/lỗi (25/11/2026)

- Xóa hoàn toàn các mesh dư/lỗi còn sót trong `Map1.glb` (`Sphere.006`, `Cube.001`, `Circle.001/002/003/009`, `Torus.001..006`, `Cylinder.001..004`).
- Tạo `WoodPlank.png` (`512×512`) + `WoodPlank.mat` (URP Lit) gán cho 10 tấm ván `Plane.009..020`, sửa scale âm `Plane.011`, và reparent ván gỗ + sỏi (`Roundcube.*`) vào đúng `Band 0..3` để rụng đồng bộ theo H01.
- Kết quả: `tools/verify.ps1 -PlayMode` → **VERIFY: PASS** (**376** EditMode, **89** PlayMode passed, **2** skipped, **0** cảnh báo `SquishyBump.*`); `python tools/check_repo.py` → **PASS** (0 problems); `tools/build-windows.ps1` → **BUILD: PASS** (**0 errors**, **3845 MB**). Chưa mở `.exe` soi lại hình trực tiếp.

## 24/11/2026 · SB-32 — kết quả, bảng điểm và spectator

Nền main `e2e9a6a`, nhánh `feat/SB-32-results-spectator`. Windows Player Direct3D12:
`ResultsSpectatorTests` **7/7 đạt, 0 skip**, gồm input Tab/trái/phải, target camera
thật ở Survival/Rank, giữ camera khi chờ hồi sinh, bảng reliable host/client qua
NGO loopback sau reconnect và snapshot mới. Kiểm 12 trang (match/session/Tab ×4
kích thước 1280×720,1920×1080,1680×720,1280×960): biên/chiều cao chữ và ribbon
không che podium. Đã xem ảnh match, session 4:3 và Tab ultrawide; không ghi
2560×1080 đạt vì OS không áp dụng. [Báo cáo và bằng chứng](testing/sb32-results-spectator.md).
Không nâng T04/T06/T07/T18 hoặc số người playtest ngoài từ các fixture này.
Verifier cuối SB-32: VERIFY PASS, 376 EditMode +89 PlayMode, 0 lỗi/cảnh báo C#,
2 skip có chủ đích (ca hình ảnh Windows-only); riêng SB-32 Player7/7 đạt.

Xác minh nguồn cuối SB-32 ngày 24/11/2026: release Windows **Succeeded: 0 errors, 3993 MB, 00:01:05.8226868**; `python tools/check_repo.py` → **PASS (0 problems)**. Không đưa 43 meta UI do importer đổi maxTextureSize vào commit.
