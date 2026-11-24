# SB-32 — kết quả, bảng điểm Tab và spectator

Nhánh `feat/SB-32-results-spectator`, nền main `e2e9a6a`, Unity 6000.3.28f1.
Issue #30, assignee ZteHahah. SB-27/PR #101 đã merge. Đối chiếu GDD VI v1.3
§4.3, §11.1–11.3 (trang 41–43), quyết định D39/D40 và kit UI đã có trên main.
Map Creative từ PR #120 giữ nguyên.

## Hành vi

- Match và session có podium top 3, tên/điểm/mạng nhận từ host. Session hiển thị
  thêm các hạng 4–6; đồng hạng không bị client tự đổi thành hạng riêng.
- Giữ delay đọc kết quả ít nhất 3 giây theo thời gian thực, kể cả timeScale=0.
- Giữ action Scoreboard (mặc định Tab) mở bảng đầy đủ; thả đóng. Bảng ghi level,
  hạng, điểm/mạng Rank hoặc trạng thái Survival. Corner board vẫn dùng kit hiện có.
- Host và client nhận hạng/điểm/mạng từ reliable MatchEvent.Scoreboard. Snapshot
  cập nhật pose/HUD cục bộ; việc avatar xuất hiện chỉ resolve tên/màu, không tự
  xếp hạng bằng mạng hoặc ghi đè điểm. Không thêm message hoặc thay wire format.
- Camera spectator áp dụng cả Survival và Rank, chỉ chọn fighter còn hiện diện,
  trái/phải đổi và quay vòng. Rank còn mạng đang chờ hồi sinh giữ camera của mình.
- HUD gameplay ẩn khi mở kết quả, giữ màn kết quả dễ đọc; Tab không tạm dừng trận.

## Kiểm chứng có thể chạy lại

`ResultsSpectatorTests` dùng scene/motor/Director thật cho input/camera. Fixture
chủ động đưa fighter qua kill plane để xác định kết quả; không phải playtest.

| Ca | Bằng chứng cần kiểm |
|---|---|
| ResultButton_WaitsThreeRealSecondsEvenWhenPaused | Nút ẩn trước mốc 3 giây, sau mốc mới hiện; callback chạy khi nhấn |
| RoundAndSession_PodiumKeepsHostRanksPointsAndLives | Top 3, điểm/mạng nhìn thấy trên cả hai trang; summary chứa đủ hạng |
| Scoreboard_HoldTabUsesConfirmedRowsAndReleaseCloses | Queue Tab qua Input System thật, frame App.Update mở/đóng HUD |
| Survival_EliminatedCameraCyclesOnlyLivingFighters | Kill plane loại người chơi, target Cinemachine thật, phải/trái/quay vòng |
| Rank_RespawnKeepsOwnCamera_EliminationEnablesCycle | Hai lần rơi còn mạng giữ camera mình; hết mạng mới spectator |
| ClientRankBoard_ReliablePointsAndRanksSurviveNewSnapshots | Hai peer NGO loopback, host gửi bảng khác thứ tự mạng; client giữ bảng sau snapshot mới |
| WindowsPlayer_ResultAndScoreboardLayoutsAtFourResolutions | Match/session/Tab ×1280×720,1920×1080,1680×720,1280×960; kiểm biên/chiều cao chữ và chụp PNG |

Fixture input dùng bàn phím giả; không gửi phím tới Windows. Trong Editor, input
được route tới Game View để chạy batchmode; background/routing cũ được khôi phục
sau mỗi ca. Save Store bị ngắt trước khi fixture đổi rule/binding; không xóa save.
Ba lần chẩn đoán fixture ban đầu chưa route keyboard vào Game View không được
coi là hồi quy gameplay. Assertions giữ nguyên cho bản chạy cuối.

Hồi quy thật đã thấy trước sửa:

1. `SessionResult_RankShowsPointsAndLivesOnVisiblePodium`: Expected True, But False
   vì podium chỉ có score; test đã tồn tại và thất bại trước sửa production.
2. Windows Player phát hiện nhãn của hàng 4–6 vượt mép: x=1324 ở màn 1280 px.
   Nhãn nay lấy chiều rộng theo hàng (hàng ngoài top 3 nhỏ hơn); không nới assertion.

Bộ test NGO cùng tiến trình tắt NGO scene management và dựng client view trong
scene host. Không dùng kết quả này để đánh dấu T06/T18 nhiều máy, T04/T07 đầy đủ
hoặc playtest người ngoài. Camera và input kiểm trên host trong scene thật;
client scoreboard kiểm qua event/snapshot thật; không tuyên bố đã chơi LAN nhiều PC.

## Lệnh

Đóng Editor đúng checkout, đặt UNITY_EDITOR đến Editor 6000.3.28f1:

```powershell
./tools/verify.ps1 -PlayMode
./tools/build-windows.ps1
python tools/check_repo.py
```

Ca hình ảnh dùng Native Unity Test Framework với `-testPlatform StandaloneWindows64`,
`-testFilter SquishyBump.Tests.PlayMode.ResultsSpectatorTests`, `-buildPlayerPath`
trỏ `Builds/SB32Visual`, `-testResults TestResults/SB32-WindowsPlayer.xml`,
`-playerHeartbeatTimeout 180`. Đây là Player development chứa test, khác release.
Ảnh gốc và log geometry tại TestResults/SB32-visual; không dùng ảnh AI/chỉnh ảnh.

Trình quét bổ sung `check_unity_project.py` báo bốn GUID script không resolve trong
DefaultVolumeProfile.asset trên main (file không đổi). Không ghi trình quét này PASS;
Native verifier và repo checker được ghi riêng. Script/metas mới do Editor sinh.
Tái dùng toàn bộ sprite/font/model hiện hữu; không thêm asset gameplay mới.


Các kiểm tra hình ảnh bổ sung: chiều rộng cột điểm/mạng ở corner board không
được làm sáu dòng biến thành nhiều dòng; ribbon không chồng bảng tên podium.
Ảnh có nút tiếp tục/về menu sau khi đợi ít nhất 3 giây, không chụp nút quá sớm.
Kích thước 2560×1080 đã thử nhưng OS không áp dụng; không ghi đạt. Dùng
1680×720 (21:9), cùng kích thước trong HudPlayerVisualTests hiện có, để kiểm
ultrawide trong giới hạn màn hình thật. 1280×960 kiểm 4:3.

Ca client có rời rồi vào lại để ID NGO khác slot prefab: tên và màu cũng khớp
host, ngoài điểm/mạng/hạng. Tab giữ ít nhất thêm sáu tick host để chứng minh
không tạm dừng gameplay.

## Windows Player đã kiểm chứng

Lượt nguồn cuối: **7 đạt, 0 lỗi, 0 skip**, renderer **Direct3D12**.
[XML Player](sb32-evidence/windows-player.xml),
[12 kiểm tra bố cục](sb32-evidence/layouts.txt),
[manifest nguồn và artifact](sb32-evidence/manifest.json).
Root timestamp XML Player là 0001 do Test Framework; dùng timestamp test-case
ngày 24/11/2026. Ảnh gốc không chỉnh sửa, dùng fixture nền tối cố định:

- [Kết quả match 1280×720](sb32-evidence/round-1280x720.png).
- [Kết quả session 1280×960](sb32-evidence/session-1280x960.png).
- [Bảng điểm 1680×720](sb32-evidence/scoreboard-1680x720.png).

Ảnh dùng tên fixture Animal/ID; luồng scene thật và ca host/client dùng các prefab
Bear/Pig… hiện hành. Đã xem trực tiếp ba ảnh trên để xác nhận chữ/biên/ribbon.
Các XML trước sửa podium/hàng/cột cũng lưu kèm để đối chiếu hồi quy.

## Cổng xác minh cuối

`tools/verify.ps1 -PlayMode` → **VERIFY: PASS**: **376 EditMode +89 PlayMode đạt**,
0 lỗi, 0 cảnh báo C#. Hai skip có chủ đích là ca hình ảnh HudPlayerVisualTests
và ca Windows Player SB-32 khi chạy Null renderer. Riêng SB-32 đã chạy đầy đủ
7/7 trong Windows Player; không tuyên bố chạy lại HudPlayerVisualTests ở Player.
[Kết quả từng ca liên quan và hash XML gốc](sb32-evidence/verify-results.json).


Ca client còn dựng MatchHost/HUD sau khi reliable bảng điểm đã đến MatchReplica.
Start lấy snapshot bảng đã cache; ViewSpawned resolve tên/màu; các snapshot mới
không ghi đè điểm. Sau đó gửi SessionResult Rank qua NGO và kiểm trang kết quả
cùng việc ẩn đúng Canvas Match HUD. Fixture đầu chọn nhầm Canvas overlay (cùng
root), đã đổi sang Canvas tên Match HUD; không thay assertion ẩn HUD.
Hai đường này bổ sung ở rà soát cuối, nên verifier/build/Windows Player được chạy
lại trên nguồn cuối trước commit.

Xác minh nguồn cuối SB-32 ngày 24/11/2026: release Windows **Succeeded: 0 errors, 3993 MB, 00:01:05.8226868**; `python tools/check_repo.py` → **PASS (0 problems)**. Không đưa 43 meta UI do importer đổi maxTextureSize vào commit.
