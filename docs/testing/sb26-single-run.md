# SB-26 — luồng Single Player và roster, 24/11/2026

Nhánh `feat/SB-26-single-roster`, nền main `1dd3328`, Unity **6000.3.28f1**.
GDD VI v1.3 §3.5–3.6 (trang 8–9), §10.5, A.6 (trang 62), T04; đối chiếu
`docs/gdd/appendix-a-ai-behaviour.md`. Quyết định D39 đã thay sáu trái bằng sáu
thú, giữ slot/hạng cân: không đưa model hay tên nhân vật cũ trở lại.

## Hành vi

- Từ lobby Single, nhân vật được chọn thuộc người chơi ID 0; năm nhân vật khác
  được shuffle theo seed một lần và gắn vào ID bot 1001–1005 suốt run.
- Level N có một bot D_N; bốn bot còn lại rút đều từ max(1,N−2) đến N. Mỗi màn
  có seed và roster bất biến. Dùng năm BotProfile hiện có, không thay thuật toán D2–D5.
- Survival thua hoặc đồng hạng nhất: **Chơi lại** cùng màn, nhân vật/ID, roster và
  seed; scene/hazard/motor/BotBrain được tạo lại. Thắng một mình: **Tiếp tục**.
- Rank tiếp tục kể cả thua, dùng cùng ScoreService. Đường production luôn từ
  lobby → L1 → … → L5; API chọn màn trực tiếp chỉ phục vụ test/công cụ.
- Sau L5 hiển thị kết quả session và số màn đã qua/số lần retry, rồi về menu.
  Giữ delay đọc kết quả ít nhất 3 s. Khi đã Result, dừng mô phỏng trận và mở chuột.
  Không triển khai cutscene SB-71 trong thay đổi này.

## Kiểm chứng đã chạy

`SingleRosterTests`: **9 ca EditMode đạt**. Sáu lựa chọn người chơi đều để lại
năm nhân vật khác nhau; 256 seed × 5 màn đối chiếu tái lập, số bot và khoảng bậc;
100 retry giữ đúng đối tượng round; Rank thua vẫn qua đủ năm màn và không vượt L5.

`SinglePlayerRunTests`: **4 ca PlayMode đạt** trên scene thật:

1. Lobby chọn slot 4 → bắt đầu L1 với đúng nhân vật và năm bot D1 còn lại.
2. Survival qua L1–L5, L2 có một lần thua và một lần hòa rồi retry. So sánh
   ID/nhân vật/tier/seed trước–sau, nút qua màn thật và tổng kết 5/5, retry=2.
3. Rank qua L1–L5 dù người chơi thua; ScoreService không bị thay giữa các màn.
4. Khi Survival thua, có nút **Chơi lại**. Ca này đã thấy đỏ trước sửa:
   `Expected collection containing "Chơi lại", But was <"Next">`.

Mỗi màn trong ca luồng chờ bot thật chạy 2 s gameplay, kiểm tra navigation/replan
và có di chuyển qua motor trước khi fixture điều khiển kết quả. Fixture dùng
kill plane để tạo thua/thắng/hòa một cách tái lập, timeScale=8; không giả lập
gọi handler kết quả hoặc tự sửa trạng thái Director thành Result.

Hai lỗi fixture được sửa trong quá trình viết test: báo ring-out bằng tick đã xử
lý nên bị Director từ chối; đặt lựa chọn trước khi Host tạo phiên nên lựa chọn bị
reset. Không tính hai lỗi fixture này là hồi quy gameplay. XML riêng ở TestResults.

**T04 giữ Một phần:** đã kiểm chứng tự động luồng đủ năm scene, retry/roster/seed
và tổng kết, nhưng kết quả match do fixture điều khiển qua kill plane. Chưa có
lượt chơi Survival L1–L5 hoàn chỉnh do người thật điều khiển chống bot. Không dùng
các ca này để đánh dấu AI-1…AI-6/T13, T07 đầy đủ, LAN hoặc playtest người ngoài.

## Chạy lại

Đóng Editor của đúng worktree này, đặt `UNITY_EDITOR` tới Editor 6000.3.28f1.
Không đóng hoặc điều khiển Editor ở checkout khác.

```powershell
. ./tools/unity-common.ps1
$code = Invoke-UnityBatch (Join-Path $RepoRoot 'TestResults/SB26-Roster.log') @(
  '-runTests', '-testPlatform', 'EditMode',
  '-testFilter', 'SquishyBump.Tests.EditMode.SingleRosterTests',
  '-testResults', ('"' + (Join-Path $RepoRoot 'TestResults/SB26-Roster.xml') + '"'))
if ($code -ne 0) { throw "Roster tests: $code" }
$code = Invoke-UnityBatch (Join-Path $RepoRoot 'TestResults/SB26-Flow.log') @(
  '-runTests', '-testPlatform', 'PlayMode',
  '-testFilter', 'SquishyBump.Tests.PlayMode.SinglePlayerRunTests',
  '-testResults', ('"' + (Join-Path $RepoRoot 'TestResults/SB26-Flow.xml') + '"'))
if ($code -ne 0) { throw "Flow tests: $code" }
```

Để chạy trong Windows Player, đổi platform thành `StandaloneWindows64`, giữ filter,
thêm `-buildPlayerPath` trỏ thư mục `Builds/SB26Visual` và
`-playerHeartbeatTimeout 180`. Framework tự chạy Player có graphics; fixture chụp
hai màn tổng kết và xuất log tại TestResults dưới project. Release không chứa test.

Hai lượt ban đầu thoát 198 trước khi có XML do license; sau khi khởi động lại Hub
hiện hữu và chờ dịch vụ sẵn sàng, Native đã chạy được. Không tính exit198 là test fail.

Trình quét bổ sung `check_unity_project.py` báo bốn GUID script chưa resolve ở
`DefaultVolumeProfile.asset` có sẵn trên main. File này không đổi trong SB-26;
không ghi trình quét đó PASS. Kết quả compile/test Native và repo checker được
ghi riêng, không thay thế bằng kiểm tra C# bên ngoài Unity.


## Windows Player, bằng chứng đã lưu

Cùng bốn ca trên đã chạy trong Windows x64 Player thật: **4 đạt, 0 lỗi, 0 skip**;
renderer **Direct3D 12, level 12.2** (Player.log), không dùng Null renderer.
[XML](sb26-evidence/windows-player.xml), [Survival log](sb26-evidence/survival-flow.txt),
[Rank log](sb26-evidence/rank-flow.txt), [hash nguồn và bằng chứng](sb26-evidence/manifest.json).
Root timestamp XML Player là 0001 do Test Framework; timestamp từng test-case là
ngày 24/11/2026, không dùng timestamp root để suy ra thời gian chạy.

Đã xem ảnh tổng kết [Survival](sb26-evidence/SB26-Survival-Summary.png) và
[Rank](sb26-evidence/SB26-Rank-Summary.png): số màn 5/5, retry Survival=2 và Rank=0
hiện ở cuối màn. Đây là ảnh ScreenCapture nguyên bản của development test Player.
Ảnh được chụp trước lúc nút về menu hiện sau 3 s; test sau đó đợi nút thật và xác
nhận về MainMenu/Idle. Layout kit và nền arena giữ từ main, không phải đợt sửa mỹ thuật.
[Hồi quy nút retry trước sửa](sb26-evidence/before-retry.xml): 0 đạt/1 lỗi.

Roster của run seed913 (ID/nhân vật giữ nguyên ở cả năm màn):

| Màn | Seed match | Bậc 5 bot |
|---|---:|---|
| L1 | 1505441284 | 1,1,1,1,1 |
| L2 | 122217686 | 2,2,1,2,2 |
| L3 | 2258762955 | 3,1,3,1,2 |
| L4 | 886483122 | 4,2,4,4,2 |
| L5 | 1428807874 | 5,3,3,4,4 |

## Fixture lobby trong bộ xác minh toàn dự án

Lượt verifier đầu đạt 356 EditMode nhưng PlayMode có 1 lỗi: test lobby đổi
Survival → Rank → Survival bằng hai lần Invoke trong cùng frame. Console vẫn giữ
snapshot Survival cũ cho lần nhấn thứ hai nên gửi Rank lần nữa. Fixture nay đợi
một frame render giữa hai thao tác, giữ nguyên hai assertion luật và gameplay.
Ca riêng sau sửa đạt 1/1 (`SB26-LobbyFixture.xml`); XML trước sửa giữ trong
TestResults/SB26-FullBeforeLobbyFixtureFix.xml. Không thay giá trị mặc định hoặc
xóa cấu hình đã lưu của người dùng để làm test xanh.

## Cổng xác minh cuối, 24/11/2026

`tools/verify.ps1 -PlayMode` → **VERIFY: PASS**: 356 EditMode đạt,
76 PlayMode đạt, 0 lỗi, 0 cảnh báo C#. Một ca HudVisual chỉ chạy trong Windows
Player được skip có chủ đích ở batchmode. Bốn ca Single Player đã chạy riêng
trong Windows Player (4/4 đạt); không dùng chúng để tuyên bố đã chạy ca HudVisual.
Kết quả từng ca và hash XML gốc lưu tại
[verify-results.json](sb26-evidence/verify-results.json).

`tools/build-windows.ps1` → **BUILD: PASS**, Windows x64 release, 0 errors,
857 MB, 00:01:23.867. Build tại `Builds/Windows/SquishyBump.exe` (không commit).
`python tools/check_repo.py` → **PASS (0 problems)**. Khôi phục riêng 43 meta UI
chỉ đổi maxTextureSize do importer hiện hữu sinh; không sửa asset UI hoặc GUID.
