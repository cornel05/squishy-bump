# SB-47 — độ khó bot và nghiệm thu AI

Ngày tiếp tục: 25/11/2026. Nhánh `feat/SB-47-bot-difficulty`, tích hợp main ban đầu
`4c9ecf0` bằng merge `9d38f62`, sau đó main `020bfb0`/PR #132 bằng merge `99be17c`. Issue #38, assignee ZteHahah; dependency #37/PR #104 đã merge.
Nguồn thiết kế: GDD VI v1.3, Phụ lục A.2–A.7 trang 61–63; D39 giữ slot/hạng cân khi đổi sang thú.

## Khôi phục WIP

WIP trước tích hợp được sao lưu nguyên tệp và binary patch tại thư mục scratch
ngoài repo, đồng thời giữ stash `aa5543cb215fb2a908d7387d648d81a2ff12d1b6`.
Không xóa stash cũ hoặc thay đổi checkout khác. Giữ roster A.6 của SB-26,
reliable bảng điểm/spectator SB-32, map Creative và chuỗi UI ASCII của main.
Fixture cũ dùng prefab trái cây đã bị thay; sau lỗi instantiate null, đổi sang
Bear/Cat/Frog và Cat/Penguin theo đúng hạng cân, không đổi seed hoặc ngưỡng.

## Hành vi

Bot chỉ chạy trên host, đọc cảm nhận trễ theo profile và xuất InputCommand qua
PlayerMotor. Giữ giá trị profile A.5, lịch quyết định 4/5/6/8/10 Hz có phần dư.
App giữ yaw input để AI đọc hướng đối thủ, không lấy góc mesh lăn.
ClassProfile có enum class được Editor API gán; PlayerMotor chỉ thêm getter đọc class.

D3 tránh head-on đối thủ nặng hơn; D4 giữ tâm với Heavy, đi sườn với Light và phanh
sớm trên băng. D5 dự đoán quãng trượt theo lực bám, dùng cảnh báo đã biết để nhử đối
thủ nặng và giảm giao tranh gần hết giờ. Không đọc lịch hazard chưa cảnh báo.
Danger băng dùng margin ×1.5 từ D3, cost A* và utility giữ công thức A.3/A.4.

## Hồi quy AI-2

Trước sửa trên main mới: D3 16/16, D4 16/17, D5 12/15 — FAIL vì D5 dưới 90%.
Ba lần D5 trễ đều đang Evade. Danger mép che giá trị cảnh báo mới; fallback chọn
chính ô đang đứng làm mục tiêu thoát, nên TTI tính một đường dài 0 m và phản ứng muộn.
D5 giờ tính thời gian trên mục tiêu ngoài các cảnh báo đã biết đang chứa footprint.
Không đổi công thức danger, thời gian cảnh báo, seed, profile hoặc tỷ lệ nghiệm thu.

`SB47-AI2-tti-clear-goal.xml`: D3 **16/16**, D4 **16/17**, D5 **15/15**; không rơi
trong 15 lượt ×60 giây, seed18–22 cố định cho mỗi bậc. Một lần trễ D4 vẫn tính vào mẫu.
Các lần cảnh báo chưa kết thúc ở giây60 ghi unresolved, không ghi thành thành công.
Đây là fixture một bot trên sàn H06/PhysX/motor thật, không phải full session/LAN.
EditMode `D5_EdgeDangerPlateau_DoesNotUseCurrentWarnedCellAsZeroLengthEscape`
kiểm mục tiêu thoát khác ô bị cảnh báo khi danger mép đồng mức.

## Trạng thái kiểm chứng

- Verifier EditMode sau tích hợp: **402/402**; sau các fixture chiến thuật **414/414**,
  sau hồi quy hệ trục mặt sàn **417/417**, đều 0 skip/lỗi/cảnh báo C#. Nguồn cuối đang chạy lại.
- Bộ AI-1–AI-6 trên nguồn sau khi bỏ API slot fixture không dùng: đang chạy,
  chưa kết luận T13. Kết quả nguồn cuối sẽ bổ sung sau khi hoàn tất.
- AI-5 bật profiler, lưu raw trong TestResults (không commit), đo toàn bộ năm
  BotBrain + dựng cảm nhận + navigation chung; không tính motor/hazard/PhysX vào
  budget logic bot. Chỉ dùng mẫu cả năm còn sống sau warmup, yêu cầu ít nhất600 mẫu.
  Số CPU ms được đo mỗi bước mô phỏng60Hz, Editor Null renderer; không suy rộng GPU/FPS Player.
- AI-6 dùng L2 thật120s với năm bot. Oracle chung bắt đầu một episode khi tiếp tục
  vận tốc hiện tại sẽ đi vào hộp hoặc làn đã cảnh báo trong khoảng phản ứng + một
  quyết định + quãng thời gian phanh. Hộp H07 dùng pose tương lai đúng lịch cho
  oracle; bot chỉ đọc snapshot trễ. Làn H03 là vùng cảnh báo đứng yên theo hint.
  Adapter chỉ quan sát impulse hazard rồi chuyển nguyên thao tác xuống physics thật.
  Episode đã nhận impulse là thất bại; contact ngoài dự báo cũng tính thất bại.
  Episode chưa giải quyết cuối cửa sổ báo riêng, không ghi đạt. Giữ ngưỡng70% riêng
  từng loại hazard và từng bậc; lưu BEGIN/END/HIT để kiểm tra mẫu số.

Không có bằng chứng LAN nhiều PC, full T04/T07/T18 hoặc playtest người ngoài từ
các fixture này. Chưa đánh dấu T13 khi còn bài chưa đạt/chưa chạy.

## Sửa oracle và các kết quả trung gian

Bộ đầu trên main mới: AI-1 đạt25 lượt; AI-2 đạt; AI-3 D5 thắng D1 **19/20**;
AI-4 D2/D1 **14/20**, D3/D2 **14/20** (1 hòa vẫn nằm trong mẫu), D4/D3 **11/20**.
D5/D4 ban đầu **4/20**, chưa đạt. Sửa dự đoán aim vượt qua chính mình và chọn hướng
sidestep giữ quán tính, không đổi tham số A.5: lượt gần nhất **13/20**, đạt55–75%.
Phải chạy lại toàn bộ trên nguồn cuối vì hành vi D5 đã thay đổi.

Oracle L2 đầu chỉ bắt đầu khi footprint đã vào vùng hộp/làn, vì thế không đếm các
lần né sớm. Thay bằng oracle quỹ đạo tiếp cận chung ở trên, không bỏ contact cũ,
không giảm ngưỡng hoặc đổi seed. Các XML trước và sau đổi oracle phải phân biệt,
không dùng tỷ lệ mẫu cũ thay cho mẫu mới. Khi sửa oracle riêng H07, D4 đạt hộp10/12,
khối12/16; D5 hộp7/9 nhưng khối29/46 vẫn FAIL. Không ghi T13 đạt ở mốc đó.

AI-5 lần đầu có dưới600 mẫu năm bot sống vì một bot bị loại sớm; không đóng băng
loại hoặc hồi sinh giả để kéo mẫu. Đo lại ba lượt đầy đủ60s, seedbot18–20, mỗi lượt
có warmup1s, chỉ lấy mẫu đủ năm bot sống, yêu cầu tổng ít nhất600 mẫu và trung bình
mỗi lượt <1ms. Lượt trung gian đạt **2315 mẫu**, mean **0.5995ms**, ba mean
**0.6366/0.5675/0.5789ms**, p95 **2.1830/1.9815/2.0501ms**; Ryzen7 6800H/16 logical
cores, Native Editor Null renderer, profiler bật và lưu raw ngoài git. Ngưỡng GDD
là trung bình, không tuyên bố từng bước hoặc p95 dưới1ms; không phải kết quả T14.

Hồi quy `MovingFloorAtGoal_RealMotorHoldsWorldPositionByCompensatingSurfaceMotion`
đỏ trước sửa: **0/2**, motor tăng vận tốc x lên0.2m/s dù bot đã tới ô an toàn.
Sau khi chụp vận tốc sàn và đổi input tương đối, **417/417 EditMode** đạt.
Giữ công thức vật lý v_target=v_surface+v_input của main, không sửa PlayerMotor.Tick.

## Nền cuối và phần cần chốt

PR #132 trên main bổ sung nước/vành H01. Đã sao lưu WIP đạt các ca đích vào stash
`ffda5e30e8ac2e5f9821c73e6e535e2a64cd6d82` và thư mục scratch riêng, tích hợp
main `020bfb0` trước verifier/build cuối. Giữ cơ chế RingDrop của main; sửa tham chiếu material rỗng như phần dưới.
EditMode nguồn tích hợp cuối: **421/421**, 0 lỗi/skip/cảnh báo C#.
Lần toàn bộ đầu: 97 PlayMode đạt, 2 lỗi toàn repo, 2 skip; AI-1–AI-6 đều đạt.
Sau sửa hồi quy bên dưới, verifier cuối đạt 421 EditMode, 99 PlayMode, 2 skip,
0 cảnh báo C#. Release Windows và smoke đã đạt; kết quả chốt ghi tiếp bên dưới.

Ca đích cuối trước tích hợp H01: D4 H07 **10/14**, H03 **25/28**; D5 H07 **9/12**,
H03 **46/49**, đều đạt70%, 0 unresolved và không có ca kẹt >3s trong cửa sổ120s.
Quy tắc chống kẹt cũng được kiểm bằng BotBrainTests.StationaryBot_RecoversToRandomSafeRoamGoal.
Các bot vẫn có thể bị loại khi đấu; báo alive cuối cửa sổ trong XML, không biến
thành yêu cầu sống đủ120s hoặc tuyên bố mọi bot sống hết thời gian.


## Hồi quy toàn repo trước lần chốt

Lần toàn bộ đầu trên `020bfb0`: EditMode 421 đạt; PlayMode 97 đạt, 2 lỗi,
2 skip do Null renderer. Cả 10 ca BotAcceptance đều đạt, nhưng verifier chưa đạt.
XML giữ kết quả từng ca và log AI/lỗi, lược log thường của các ca khác, tại `before-global-fixes.xml`.

- L1 có 19 renderer Roundcube thiếu material. Đối chiếu JSON nguồn `Map1.glb`:
  các primitive này không khai báo material. Material mặc định do glTFast sinh
  không có asset bền vững; scene cần bản sao persistent. Editor API tạo
  `Art/Levels/L1/MapDefault.mat` từ chính MaterialGenerator của glTFast hiện hữu,
  rồi chỉ thay 19 tham chiếu rỗng. Builder cũng gán bản persistent khi tái tạo.
  Không đổi mesh, transform, collider hoặc RingDrop; assert material vẫn giữ nguyên.
- Fixture lobby chờ Room công bố lựa chọn nhưng avatar neutral chỉ được thay ở
  tick sau, nên đôi lúc dịch chuyển nhầm body cũ. Chờ đúng avatar đã chọn rồi mới
  đưa host tới quầy; giữ timeout 10s và các kiểm tra gameplay. Ca đích sau sửa đạt.

Đây là hai lỗi lộ ra khi chạy cổng nghiệm thu chung, không tính là các ca AI đã
thất bại hoặc dùng để hạ chuẩn T13. Cần lần verifier/build cuối sau các sửa này.


## Kết quả nguồn cuối ngày 25/11/2026

`tools/verify.ps1 -PlayMode` → **VERIFY: PASS**: 421 EditMode đạt, 99 PlayMode
đạt, 0 lỗi và 0 cảnh báo C# SquishyBump.*, 2 skip do Null renderer. Cả 10 ca
BotAcceptanceTests đạt. Nguồn tích hợp base `020bfb0`, merge `99be17c`, delta SB-47
được fingerprint bằng `sb47-evidence/manifest.json`; XML kết quả trong cùng thư mục.

| Tiêu chí A.7 | Kết quả cuối |
| --- | --- |
| AI-1 | 25 lượt (5 bậc × seed18–22), 120s/lượt, không rơi trong cả 25 lượt |
| AI-2 | D3 16/16, D4 16/17, D5 15/15; không rơi sớm trong 15 lượt |
| AI-3 | D5 thắng D1 18/20 (90%) |
| AI-4 | Bậc cao thắng 14/20, 14/20, 11/20, 13/20; cặp D3/D2 có 1 hòa nằm trong mẫu 20 |
| AI-5 | 2315 mẫu đủ năm bot sống, trung bình 0.6259ms; ba mean 0.7026/0.5729/0.5680ms |
| AI-6 | D4 H07 10/14, H03 25/28; D5 H07 9/12, H03 46/49; 0 unresolved, không kẹt >3s |

AI-5 p95 từng lượt 2.5733/2.0786/2.0145ms; chỉ ngưỡng trung bình <1ms được đạt,
không tuyên bố p95 dưới1ms. Phạm vi vẫn là Editor Null renderer trên Ryzen7 6800H,
profiler bật, ba lượt đủ60s với loại tự nhiên. Đây không phải T14/Player FPS.

T13 đạt tự động theo bộ A.7 trên nguồn cuối. Checklist issue #38 lưu 4/4 và đọc lại
bằng connector ngày25/11 lúc23:25; bình luận bằng chứng 7167704571. Issue vẫn mở,
chưa coi feature hoàn tất/merge khi còn chờ PR và review cornel05.
Hai skip là các test hình ảnh HUD/Results cần renderer, không phải ca AI bị bỏ qua.
Chưa có bằng chứng LAN nhiều PC, playtest ngoài nhóm, hoặc full T07 từ bộ này.


Kiểm tra GUID bổ trợ: 783 tệp, 4 chẩn đoán missing-script có sẵn tại
`Settings/URP/DefaultVolumeProfile.asset`, 0 warning. Bốn lỗi trùng nguyên trạng
baseline SB-32; file URP không đổi so với origin/main. Không ghi scanner GUID PASS;
verifier/test/runtime và repo checker là các kết quả riêng. Không thêm lỗi GUID mới.
Release Windows: **BUILD: PASS**, **Succeeded: 0 errors, 3845 MB, 00:06:40.0110304**.

Smoke bản Windows vừa build: headless 50 giây, managed assemblies tải xong,
process còn chạy, 0 exception, save.json không đổi; chỉ dừng PID smoke do công cụ
tạo. Log/JSON trong sb47-evidence; không dùng smoke này làm bằng chứng gameplay,
renderer, LAN hoặc FPS. Chờ cornel05 review PR; không tự merge.
