# SB-17 — bằng chứng sửa review PR #97, 23/11/2026

Đối chiếu [review của cornel05](https://github.com/cornel05/squishy-bump/pull/97#pullrequestreview-6011519120)
và issue #15. GDD tiếng Việt v1.3 §11.2 (PDF trang 42): HUD cố định chừa giữa màn hình;
Wobble vẫn phải ở phía trên từng quả. Giữ nguyên anchor HUD đã được reviewer xác nhận.

## Sửa hành vi

Điểm bám Wobble lấy phía trên khung model chiếu lên màn hình, dùng local bounds có
biến đổi để tránh phóng đại khung khi model lăn. Pivot ở đáy nhãn. Nhãn được nâng
lên khi đụng hình quả khác hoặc chữ đã đặt; không đẩy nhãn xuống che quả. Khi không
còn đủ chỗ trong viewport, nhãn bị ẩn. Không thay đổi dữ liệu gameplay hoặc màu/ngưỡng Wobble.
Trong cụm đông, nhãn có thể cách đầu quả xa hơn để tránh che nhau; cần reviewer đánh
giá độ dễ nhận biết qua ảnh và khi chơi.

## Windows player thật

Unity 6000.3.28f1, Windows x64 test Player, **Direct3D12**, không dùng Null renderer.
`HudPlayerVisualTests.WindowsPlayer_HudAndTrackedTags_AspectSweep`: **1 đạt, 0 lỗi**,
XML tại [windows-player.xml](windows-player.xml). Đây là development Player có test assemblies;
chữ Development Build trong ảnh là watermark bình thường. Test không được đóng vào release.

Nạp Boot → Single Survival → L1 thật, đủ sáu prefab. Đặt Wobble 0/2/4/6/8/10 qua
CollisionResolver rồi đợi host cập nhật HUD. Freeze để chụp; cảnh packed đặt sáu quả
thành hai hàng sát nhau và cố định camera. Ảnh là ScreenCapture nguyên bản, đã xem
trực tiếp cả tám ảnh. JSON đo glyph/outline, khung model và khung HUD; không chỉ đo anchor.

| Kích thước | Tỉ lệ | Cảnh thường | Cụm sáu quả | Kết quả |
|---|---|---|---|---|
| 1920×1080 | 16:9 | [PNG](normal-1920x1080.png) | [PNG](packed-1920x1080.png) | 6 nhãn/cảnh, 0 giao nhau |
| 1280×720 | 16:9 | [PNG](normal-1280x720.png) | [PNG](packed-1280x720.png) | 6 nhãn/cảnh, 0 giao nhau |
| 1440×900 | 16:10 | [PNG](normal-1440x900.png) | [PNG](packed-1440x900.png) | 6 nhãn/cảnh, 0 giao nhau |
| 1680×720 | 21:9 | [PNG](normal-1680x720.png) | [PNG](packed-1680x720.png) | 6 nhãn/cảnh, 0 giao nhau |

Tất cả JSON cùng tên PNG: `fixedHudClear=true`, `visibleTags=6`,
`ownBodyOverlaps=[]`, `labelOverlaps=[]`, `otherBodyOverlaps=[]`.
HUD cố định nằm ngoài vùng giữa 25%–75% theo cả hai trục. Wobble vẫn được phép
theo quả vào giữa theo GDD; chữ không đè hình quả trong các cảnh đã đo.
Sau ma trận, test dịch quả 0,8 m và xác nhận nhãn đổi vị trí, đồng thời xác nhận ×8 đỏ.

Hồi quy đã thấy đỏ trước sửa, mỗi lượt 0 đạt/1 lỗi:

- [before-own-body.xml](before-own-body.xml): nhãn cắt qua hình chính quả đó ở 1920×1080.
- [before-tag-collision.xml](before-tag-collision.xml): hai glyph Wobble đè nhau trong cụm sáu quả.
- [before-other-body.xml](before-other-body.xml): nhãn che quả khác; [ảnh trước sửa](before-other-body-1920x1080.png).

Hash nguồn UI và từng bằng chứng lưu trong [manifest.json](manifest.json).

## Chạy lại

Đóng Editor, đặt `UNITY_EDITOR=D:\Tool\Unity\6000.3.28f1\Editor\Unity.exe`, chạy từ repo:

```powershell
. ./tools/unity-common.ps1
$code = Invoke-UnityBatch (Join-Path $RepoRoot 'TestResults/HudWindowsPlayerFinal.log') @(
  '-runTests', '-testPlatform', 'StandaloneWindows64',
  '-testFilter', 'SquishyBump.Tests.PlayMode.HudPlayerVisualTests',
  '-buildPlayerPath', ('"' + (Join-Path $RepoRoot 'Builds/HudVisual') + '"'),
  '-testResults', ('"' + (Join-Path $RepoRoot 'TestResults/HudWindowsPlayerFinal.xml') + '"'),
  '-playerHeartbeatTimeout', '180')
if ($code -ne 0) { throw "Windows Player test failed: $code" }
```

Editor batchmode dùng nographics để build, còn Player do Test Framework chạy có graphics.
PNG/JSON sinh tại `TestResults/SB17-Visual/`. Không lấy kết quả skip trong Editor làm
bằng chứng Windows player. Bộ verify toàn dự án và release build ghi ở docs/testing.md.

Đây là fixture hình ảnh có kiểm soát trên L1, không phải một lượt chơi tay liên tục,
LAN nhiều PC, T07 đầy đủ hoặc playtest người ngoài. Giữ PR draft để cornel05 đối chiếu
ảnh và review; chưa merge hay tuyên bố issue hoàn tất theo Definition of Done.


Bộ cuối trên nền main c534179, protocol LAN-10: **252 EditMode + 61 PlayMode**
đạt, 0 lỗi/cảnh báo compile; 1 skip Windows-only đã chạy riêng đạt 1/1. Từng ca và hash XML gốc:
[verify-results.json](verify-results.json). Release Windows x64 **101 MB,
0 errors, 51,49 s**, BUILD: PASS. Toàn bộ ảnh/JSON/XML Windows trong thư mục là
lượt chạy lại sau tích hợp lobby; nguồn MatchHud không đổi giữa hai lượt.
