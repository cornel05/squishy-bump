# SB-29 — bảng loại Survival LAN và bằng chứng, 24/11/2026

Nhánh `feat/SB-29-survival-evidence`, nền main `3cef5f0`, Unity 6000.3.28f1.
Gameplay đã merge qua PR #107 (`734d018`); SB-24/PR #99 và SB-27/PR #101 cũng
đã merge. Nhánh này bổ sung kiểm chứng và đối chiếu nghiệm thu.

## Đặc tả áp dụng

GDD VI v1.3 §3.5–3.6 (trang 8–9), T18 (§14.3), cùng quyết định D38/#106
ngày 23/11/2026: áp dụng số loại ceil((N−1)/L_left), giữ tối thiểu hai người ở
các màn trước màn cuối do host chọn. Nếu đồng hạng cắt ngang ngưỡng loại,
đấu lại cùng màn, roster đủ điều kiện, nhân vật và seed. Màn cuối cần đúng
một champion; người bỏ cuộc mất suất. D39 dùng các thú mới theo slot hiện có.
Không thay đổi thiết kế hoặc thuật toán production trong nhánh này.

## Bằng chứng theo tiêu chí

| Tiêu chí issue #27 | Bằng chứng tự động | Giới hạn |
|---|---|---|
| Loại người hạng thấp theo công thức | `LanSurvivalRunTests.EveryRoomSizeAndLength_RemovesWorstPlayersAndEndsWithOneChampion`: 25 tổ hợp N=2…6, Lk=1…5, bảng số người còn được viết độc lập | Bao gồm ngoại lệ giữ hai người D38 |
| Duy nhất một champion cuối phiên | Cùng bảng kiểm đối chiếu toàn bộ thứ hạng và danh tính; ca hòa ngưỡng/chung kết hiện có | Bỏ cuộc có thể kết thúc sớm theo thiết kế |
| Người bị loại spectate | `LobbyPlayTests.LanSurvival_HostKnockedOut_SpectatesWhileClientsPlayFinal`: loại host L1, L2 chỉ spawn hai client, hiện ĐANG QUAN SÁT, nhận kết quả bỏ cuộc | NGO loopback cùng tiến trình |
| T18 pass | Chỉ một phần: model 6 người/5 màn, 4 người/3 màn; loopback hai người/5 màn và ba người/2 màn | Chưa chạy hai cấu hình T18 với người thật/PC riêng |

Bảng kiểm mới gồm cả phòng 3/5 người và các độ dài trước đây chưa có case.
Không tính lại công thức production trong assertion. Người chơi được xếp thứ tự
ngược ID: host có thể bị loại sớm; kiểm đúng tập sống sót, thứ hạng cuối liên tục,
không trùng danh tính và đúng một hạng 1.

Các ca PlayMode tái dùng từ main:

- `LanSurvival_TwoPlayersContinueThroughL5_AndClientReceivesChampion`: giữ ID/nhân vật
  qua năm scene thật, client nhận MatchResult và SessionResult đúng thứ hạng.
- `LanSurvival_HostKnockedOut_SpectatesWhileClientsPlayFinal`: host bị loại trước
  chung kết, quan sát hai client, bảng kết quả giữ cả host sau khi client bỏ cuộc.
- `LanSurvival_DrawAtFinal_ReplaysLevelUntilThereIsOneChampion`: hòa thì nạp lại
  cùng scene/roster/seed, sau trận phân thắng bại client nhận một champion.

Loopback tắt NGO scene management và nạp scene trên host; chưa chứng minh đồng bộ
chuyển scene giữa các Player/PC. Kết quả match được fixture điều khiển qua kill
plane để tái lập; không phải playtest nhiều người.

## Chạy lại và phần cần nghiệm thu

Từ repo root, đóng Editor đúng checkout; đặt UNITY_EDITOR tới bản 6000.3.28f1:

```powershell
./tools/verify.ps1 -PlayMode
python tools/check_repo.py
```

Bản release Windows của cùng nguồn runtime `3cef5f0` đã build trong SB-26:
0 lỗi, 857 MB; xem báo cáo SB-26. Nhánh này chỉ thay test và tài liệu, không tuyên
bố đây là một build release mới hoặc lượt T18 trên build đó.

Để hoàn tất T18: chạy 6 người L1–L5 và 4 người L1–L3 trên PC riêng, ghi phiên bản
build/commit, ngày, máy/participant, ID/nhân vật, người bị loại sau mỗi màn và bảng
kết quả mọi máy; xác nhận người bị loại spectate và đúng một champion. Có thể dùng
bảng mẫu dưới đây; để trống cho tới khi có dữ liệu thật.

| Cấu hình | Build, ngày, máy | Sau từng màn | Champion và hạng mọi máy | Kết quả |
|---|---|---|---|---|
| 6 người, 5 màn | Chưa chạy | Chờ dữ liệu | Chờ dữ liệu | Chưa kiểm chứng |
| 4 người, 3 màn | Chưa chạy | Chờ dữ liệu | Chờ dữ liệu | Chưa kiểm chứng |

Giữ T18 chưa tick và PR nháp trong khi thiếu lượt nhiều máy. Không dùng các ca
này để nâng T06/T07/T11/T12 hoặc số người playtest bên ngoài.

## Kết quả lượt 24/11/2026

`tools/verify.ps1 -PlayMode` → **VERIFY: PASS**: 381 EditMode đạt (32 ca
LanSurvivalRunTests, gồm 25 tổ hợp mới), 76 PlayMode đạt, 0 lỗi, 0 cảnh báo C#.
Một ca HudVisual dành riêng Windows Player được skip có chủ đích ở batchmode;
không ghi nó đã chạy trong lượt này. Ba ca Survival lobby nêu trên đều đạt.
`python tools/check_repo.py` → **PASS (0 problems)**.

[Kết quả từng ca liên quan và hash nguồn/XML gốc](sb29-results.json).
Nguồn hash chuẩn UTF-8 LF; TestResults giữ XML/log đầy đủ tại checkout chạy test.
43 meta UI tự đổi maxTextureSize bởi importer main đã được đối chiếu và khôi phục
riêng sau khi Editor đóng. Không đổi asset UI, runtime hay GUID.
