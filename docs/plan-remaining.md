# Phần còn lại của plan (cornel05) — bàn giao cho session khác

## Cập nhật 21/11/2026

`main` = `c293fe1`. Phần dưới lưu lịch sử bàn giao cũ.

| Bước | Kết quả |
|---|---|
| 0 · LAN recovery | [PR #91](https://github.com/cornel05/squishy-bump/pull/91) squash-merged (`e848f20`, `Refs #18`). Verify 162 EditMode + 36 PlayMode, 0 cảnh báo, repo check PASS. #18 vẫn mở để Nhật retest. Diff local `EditorBuildSettings.asset` chỉ thêm `Test_Duy` và được giữ ngoài PR. |
| C1 · Frame recorder | [PR #93](https://github.com/cornel05/squishy-bump/pull/93) squash-merged (`9bbe86d`, `Refs #43`). Verify 165 EditMode + 36 PlayMode, 0 cảnh báo, repo check và Windows build PASS. L3 PC 1080p, 30 s, 23.313 mẫu, 869,79 FPS median, p95 1,803 ms. CSV và summary nằm trong `Application.persistentDataPath/perf/`. T14 vẫn chờ L5 và đo bot. Một số vật thể rìa sân lên màu magenta trong Windows player. |
| C2 · T15 | [PR #94](https://github.com/cornel05/squishy-bump/pull/94) squash-merged (`c293fe1`, `Refs #45`). MPP L3, simulator danh định 100 ms RTT / 2% loss, UTP báo 146 ms. Client phản hồi 6/6 lần sau 344–354 ms, nên không đạt tiêu chí tức thời. Sáu lần đầu hiện nón gió khớp tick render và hướng. Log và probe lưu trong `squishy-bump-plan-work/TestResults/`. |
| C3–C4 · Playtest, README, demo | [PR #92](https://github.com/cornel05/squishy-bump/pull/92) squash-merged (`15c78a5`, `Refs #44/#47/#51`). Có form E.5/E.6, bước chạy Windows, giới hạn LAN và kịch bản demo. Playtest ngoài nhóm, test máy sạch và diễn tập còn chờ. |

Các issue chính vẫn mở. #26 chờ #25. #43 chờ #36 và #38. #44 chờ #36.
#45 chờ #38. #46 chờ #22 và #25. #47 chờ #45. #51 chờ #47, và các đoạn
lobby, Rank, bot trong kịch bản còn phụ thuộc #23, #25, #26 và #38.

Cập nhật: 16/11/2026 (cuối ngày). Deadline A02: **25/11/2026**. File này là ghi chú bàn giao, chưa commit.

## 1. Trạng thái hiện tại

`main` = `8ef6ee9`. Verify lần cuối: 137 EditMode + 33 PlayMode pass, 0 warning, `check_repo` PASS.
Build Windows PASS (100 MB); exe chạy batchmode 30 s, Boot → MainMenu, không lỗi.

Đã squash-merge hôm nay (agent được phép tự merge sau khi verify + check_repo PASS):

| PR | Issue | Nội dung | Issue còn mở vì |
|---|---|---|---|
| #68 | SB-13 #11 | RollVisual, CameraRig (Cinemachine), PresentationProfile | — |
| #69 | SB-15 #13 | H01 RingDrop + prefab `TrainingRingFloor`, host loop `MatchHost`, sandbox `Test_Duy` | — |
| #70 | SB-28 #26 | `RespawnPicker` (hàm thuần) | chờ nối vào luật Rank khi SB-27 (#25, Nhật) xong |
| #71 | SB-21 #19 | Input lên 60 Hz / snapshot xuống 30 Hz, buffer 100 ms, `MatchClientView`, `DevSessionPanel` | 2/4 box chờ chạy LAN 2 instance |
| #72 | SB-23 #21 | Hazard event reliable qua mạng, telegraph client theo host tick | 1/2 box chờ chạy LAN |
| #73 | SB-31 #29 | Main menu, Host/Join IP, rule, Settings, Esc pause, `LevelLauncher`, `MatchSetup` | — |
| #74 | SB-40 #31 | L2: H02 turntable + H07 obstacle ring, `IHazard`/`IFighterPhysics`/`ISurfaceMotion` | — |
| #75 | SB-41 #32 | H03 pusher blocks | — |
| #76 | SB-42 #33, SB-43 #34, SB-19 #17 | L3: pháo gió + pháo lửa; T01 pass | — |
| #77 | SB-48 #39 | Arena card, hazard banner, cảnh báo 10 s cuối | — |
| #78 | SB-49 #40 | `AudioDirector`, 12 SFX placeholder (`tools/audio/make_sfx.py`), hit-stop, màu telegraph GDD 12.5 | — |
| #79 | SB-51 #42 | Save JSON (settings, binding, trái cuối, kỷ lục) | — |
| #80 | (SB-60 #47) | README: LAN quick-start, known limits | SB-60 chờ SB-54 |
| #90 | (SB-21 #19) | Sửa đóng băng LAN: hit-stop khi menu Esc LAN đang mở để host ở timeScale 0; Duy test lại OK 20/11 | — |
| #86 | SB-11 #9 | 6 trái `.glb` (Blender 4.2, `tools/models/make_fruits.py`) + glTFast 6.20.0 | — |
| #85 | (SB-31 #29) | Single Player chọn level L1–L5; level thiếu `TrainingRingBounds` quay về menu | — |
| #84 | D35–D37 | Ghi cắt SB-22, SB-70, SB-72 (#81–#83); #20, #52, #54 đóng not planned | Nhật xác nhận trong #81–#83 |

Blender 4.2 cài ở `D:\Program Files\Blender Foundation\Blender 4.2\blender.exe` (chạy lại `make_fruits.py` khi đổi model).

## 2. Việc còn lại của cornel05

| Issue | Việc | Chặn bởi |
|---|---|---|
| #19, #21 | Chạy LAN 2 instance (README "LAN quick-start"), tick box còn lại | người chạy |
| #26 SB-28 | Gọi `RespawnPicker` + `resolver.ResetWobble` + bất tử trong luật Rank | #25 SB-27 (Nhật) |
| #43 SB-52 | Đo FPS Chaos Core (T14), bot < 1 ms | #36 SB-45, #38 SB-47 (Nhật) |
| #45 SB-54 | Chạy toàn bộ P0 test, ghi test record | cả hai, cần nhiều phần của Nhật |
| #46 SB-55 | LAN nhiều máy (T06) | người + máy thật |
| #44 SB-53 | Playtest người ngoài | người thật |
| #47 SB-60 | Build + thử máy sạch theo README | #45 SB-54 |
| #51 SB-64 | Demo + vấn đáp | cuối |

## 3. Việc của Nhật đang ảnh hưởng

| Của Nhật | Ghi chú cho Nhật (trong PR "For @ZteHahah") |
|---|---|
| #12 L1 | Thay 4 `Band …` bằng `Prefabs/Hazards/TrainingRingFloor.prefab` (hiện L1 vẫn chạy được vì `LevelLauncher` tìm `RingDrop` trong scene; nếu không thay thì L1 không có ring drop) |
| #35 L4, #36 L5 | Scene cần `TrainingRingBounds` để `LevelLauncher` mở trận; L5 thêm `Cannons` (Air 270°, Fire 90°, `chaosPeriod`) |
| #25 Rank | gọi `bootstrap.RecordRank`, publish `MatchEvent.Respawn` (có sẵn âm thanh) |
| #23 Lobby | thay nút "Start match" bằng lobby; ghi `Saved.LastFruit` |
| #38 Bot | cấp `InputCommand` cho fighter id ≥ 1000 trong `MatchHost` |
| #48 Credits | thêm 12 `sfx_*.wav` (procedural, tự tạo) |

## 4. Bẫy đã gặp (đọc trước khi code)

- Viết `UnityEngine.Physics.Raycast(...)`, không `Physics.` (namespace `SquishyBump.Physics` che).
- `SessionState` trùng tên với `UnityEditor.SessionState` trong test có `using UnityEditor` → viết `Core.SessionState`.
- Compile lỗi trong batchmode để lại `Temp/UnityLockfile` → `verify.ps1` tưởng Editor đang mở. Hook không cho xoá
  file đó: chạy lại `ProjectScaffold.Run` bằng batchmode (thoát sạch thì lockfile tự mất), rồi verify.
- NGO đưa `NetworkManager` (bootstrap) sang DontDestroyOnLoad: test load Boot nhiều lần để lại nhiều bootstrap →
  teardown phải Destroy, test chọn bootstrap có `Launcher`. Test level phải unload scene level khi xong.
- Test không được ghi save thật: đặt `app.Store = new LocalSave(<temp>)`.
- Sandbox scene không có trong build list: load bằng `EditorSceneManager.LoadSceneAsyncInPlayMode`.
- Collider chỉ để nhìn: `DestroyImmediate` (Destroy trễ làm `GetComponentsInChildren` lấy object đã huỷ).
- Kiểm chạm giữa hộp xoay dùng `Physics.ComputePenetration`, không `Bounds.Intersects` (AABB quá to).
- Chuỗi YAML có `: ` phải đặt trong `'…'` khi sửa `.asset` bằng tay.
- Python sửa file: `encoding='utf-8'`, `newline=''`; không dùng `sed` cho chuỗi có `\` (GNU escape).
- Số mới: field trong SO (`HazardProfile`, `PresentationProfile`, `MatchSetup`, `BalanceProfile`) + dòng asset + assert.

## 5. Quy trình mỗi issue

`issue-start` → code + test → `tools/verify.ps1 -PlayMode` + `python tools/check_repo.py` → `issue-finish`
(tick box, `docs/testing.md`, PR `Closes #n`, mục "For @ZteHahah" nếu đụng Core/App) → squash merge.
