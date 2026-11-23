# Credits và khai báo AI

Kiểm kê ngày 20/11/2026 trên main `b856485`, phục vụ SB-61 #48, GDD 15.2 và Phụ lục C.
Đây là hồ sơ nguồn của checkout, chưa xác nhận nội dung của một build phát hành.

## Nhóm thực hiện

BKU Group 18: Đình Duy (`cornel05`) và Minh Nhật (`ZteHahah`).
Phân công hiện hành nằm trong `AGENTS.md`; tác giả và các lượt sửa được truy vết bằng Git history.

## Asset đang dùng trong project

| Nhóm | Nguồn, công cụ và sử dụng | Khai báo |
| --- | --- | --- |
| Sáu quả `.glb` | `tools/models/make_fruits.py`, Blender 4.2 LTS; commit `b856485`, PR #86; prefab fighter tại `Assets/_Project/Prefabs/Fighters/` | Mô hình procedural có Claude hỗ trợ viết script. Không phải ảnh text-to-image và không nhập model bên ngoài. Seed Blueberry 18, các quả khác dùng mặc định 0; không có bản lưu prompt nguyên văn trong repo. |
| 12 SFX `.wav` | `tools/audio/make_sfx.py`, Python stdlib; commit `3d22d10`, PR #78; `AudioDirector` / `PresentationProfile` | Tổng hợp bằng code có Claude hỗ trợ, seed 18, 44.1 kHz mono PCM 16-bit. Placeholder, không lấy sample âm thanh ngoài. |
| L1 và vật liệu greybox | Mesh/material dưới `Data/TrainingRing/`, `ProjectScaffold` và dữ liệu mesh đã commit (script tạo mesh ban đầu không có trong checkout); Phụ lục C đã ghi nguồn SB-14 | C# và Unity 6000.3.28f1 với Codex hỗ trợ. Không dùng seed ngẫu nhiên hay model tạo ảnh. |
| L2/L3 và hình hazard | `LevelSceneBuilder`, `MovingGround`, `PusherBlocks`, `Cannons`, `AnnulusMesh`, `RingDrop` | Primitive, mesh và màu tạo bằng C#/Unity với AI hỗ trợ code. Không có texture, model hoặc sample tải ngoài trong nhóm này. |
| Giao diện | `UI/Menus/UiFactory.cs` tạo panel và chữ bằng uGUI | Font chính là Sleeping (Darrell Flood, `Assets/ThirdParty/Fonts/Sleeping/`; miễn phí cho mục đích cá nhân, cần mua giấy phép nếu thương mại hóa). Sleeping thiếu dấu tiếng Việt nên Arial/Segoe UI làm font dự phòng. Sprite UI do nhóm vẽ: nút, khung máy chơi game, màn Start, HUD, tổng điểm round và WINNER lấy từ thư mục Drive "Assets cho UX/UI" (PNG cắt sẵn) bằng `tools/ui/import_uxui.py`; slider, toggle, panel, huy chương, avatar cắt từ Buttons.ai bằng `tools/ui/slice_kit.py`. Không nhập ảnh `[Demo]` vì nền có ảnh chụp của bên thứ ba. |
| HUD SB-17 | `UI/HUD/MatchHud.cs`, phiên bản 21/11/2026 | Codex hỗ trợ C# sinh vòng cooldown Texture2D 64 × 64, huy hiệu class bằng uGUI và chữ LegacyRuntime. Không dùng ảnh tải ngoài hoặc mô hình sinh ảnh; nguồn yêu cầu là issue #15/GDD 11.2, không dùng seed. |
| Kết quả SB-24 | `UI/Menus/MatchOverlay.cs`, 21/11/2026 | Codex hỗ trợ code trình bày standings host bằng uGUI (từ SB-74 dùng sprite/font UI kit); không thêm ảnh, font tải ngoài hoặc model sinh ảnh. Yêu cầu: issue #22, GDD 10.4/11.3; không dùng seed. |

| Lobby runtime SB-25 | `App/LobbyController.cs`, `UI/LobbyPanel.cs`, `Data/LobbyProfile.asset` · 21/11/2026 | Codex hỗ trợ C#/Unity primitive sphere/material URP cho avatar trung tính, uGUI/LegacyRuntime cho info/controls, nhãn khóa kệ và quầy host. Avatar đã chọn/display dùng lại sáu prefab/GLB nêu trên. Không có ảnh AI, model/font tải ngoài hoặc seed hình học; nguồn yêu cầu là issue #23, GDD 8.5/11.1. |
| Nhạc | Không tìm thấy clip nhạc trong `Assets/` | Chưa nhập nhạc. Không gán tác giả/giấy phép cho nội dung chưa có. |

Danh sách từng tệp, hash SHA-256, nguồn, vị trí dùng và giới hạn thông tin nằm tại
[`docs/asset-ledger.csv`](docs/asset-ledger.csv). Các tài sản do nhóm tạo không tự động được
cấp một giấy phép nguồn mở; repo chưa có LICENSE chung. Thành phần Unity, shader URP,
font tích hợp và package vẫn theo giấy phép tương ứng của nhà cung cấp.

## Thành phần bên ngoài

Unity Editor 6000.3.28f1, URP 17.3.0, uGUI 2.0.0 (font tích hợp), Input System 1.20.1,
Cinemachine 3.1.7, Netcode for GameObjects 2.13.3, Unity Transport 2.6.0,
glTFast 6.20.0 và các package phát triển/kiểm thử được khóa trong `Packages/manifest.json`
và `Packages/packages-lock.json`. Không coi chúng là tài sản của nhóm.
Các thông báo LICENSE/Third Party Notices đi kèm package cần được giữ khi đóng gói;
chưa kiểm tra bản build cuối nên chưa xác nhận các notice đã được phát hành đầy đủ.

Blender và Python là công cụ tạo asset, không phải model hay thư viện âm thanh được nhập vào game.
Không áp giấy phép phần mềm Blender cho model đầu ra chỉ vì model được xuất từ Blender.

## Hình minh họa GDD

Phụ lục C.1 giữ danh mục A01–A15 và F1–F4 của bộ tài liệu thiết kế. A01 và A05 L1 được
GDD khai báo là ảnh Blender tinh chỉnh bằng FLUX.1 Kontext [dev], seed lần lượt 7001 và 7005;
GDD ghi giới hạn phi thương mại. Baloo 2/Fredoka được GDD ghi SIL OFL 1.1.
Đây là thông tin từ GDD; checkout hiện tại không chứa bộ nguồn `_src/`, prompt đầy đủ,
file license của bộ ảnh hay hai font nên chưa xác minh lại độc lập các hồ sơ đó.
Các ảnh thiết kế không phải ảnh chụp game hay bằng chứng nghiệm thu.

## AI trong code và tài liệu

Claude Code hỗ trợ Đình Duy, Codex hỗ trợ Minh Nhật trong thiết kế, C#, Editor tooling,
test, script sinh model/SFX và tài liệu. Phạm vi truy vết bằng commit, issue và PR;
không có log prompt/model-version đầy đủ cho mọi lượt sinh code nên không tự điền phiên bản hay seed.
Hai thành viên chịu trách nhiệm đọc, kiểm thử và giải thích phần mình nộp.
Khai báo dùng AI không đồng nghĩa mỗi dòng đã được người thật review hoặc mọi tính năng đã đạt test.

## Việc còn phải đối chiếu trước khi nộp build

- Đối chiếu lại CSV với commit được build sau khi các PR asset mới merge; giữ lịch sử khi thay asset.
- Kiểm tra Credits trong game và các notice thực sự có trong thư mục build.
- Nếu đưa hình/font từ bộ GDD vào runtime, bổ sung tệp nguồn, license và prompt gốc khi nguồn cung cấp.
- Chạy compile/PlayMode và build Windows sau khi khôi phục license Unity; lượt ngày 20/11 thoát 198,
  `No valid Unity Editor license found`, chưa có kết quả kiểm thử mới.

## Bổ sung SB-44 — H06 Frost Ring, 21/11/2026

- `H06-PROC-001`: mesh hex prism 2 m, màu băng trong suốt và vết nứt trắng lan dần bằng
  Unity Mesh API, MaterialPropertyBlock, LineRenderer; runtime tại `Scripts/Hazards/IceTiles.cs`.
  Nhóm 18 tạo với hỗ trợ AI Codex theo SB-44, GDD 7.4/9.2.4; không dùng ảnh hoặc model bên ngoài.
  Hình học không dùng RNG; lựa chọn ô dùng seed trận do host cung cấp.
- `H06-CONFIG-001`: `Data/Hazards/IceProfile.asset` và scene `Scenes/Levels/L4.unity`
  do Unity Editor 6000.3.28f1 sinh qua `Editor/IceSceneBuilder.BuildL4`; thông số từ GDD.
  Không có prompt image generation hoặc seed asset. Không khai nhận giấy phép chung của repo;
  shader URP thuộc package Unity hiện hữu, theo điều khoản nhà cung cấp.
- Native kiểm tra hành vi/collider, không thay thế kiểm tra hình ảnh, khả năng đọc cảnh báo
  hoặc LAN trên các PC riêng. Bản ghi CSV lưu hash và vị trí cụ thể của từng nguồn.

H06-PROC-001 source v3: bổ sung lịch sử chuyển trạng thái theo thời gian host để
client nội suy không hiện sàn hồi quá sớm khi nhận warning chu kỳ mới. Source v2
và hash cũ được giữ trong lịch sử Phụ lục C; không đổi asset/nguồn bên ngoài.

## Bổ sung SB-45 — L5 Chaos Core, 21/11/2026

`ENV-L5-001`: scene `Assets/_Project/Scenes/Levels/L5.unity` được Unity 6000.3.28f1
sinh qua `Editor/IceSceneBuilder.BuildL5`, có hỗ trợ AI Codex theo issue SB-45 và GDD
7.5/9.2.5. Tái dùng H06-PROC-001/H06-CONFIG-001 của L4 và Cannons/HazardProfile có sẵn
trên main (H04/H05); không thêm ảnh, texture, âm thanh hoặc model bên ngoài. Mount
và hình học không dùng RNG; chọn ô dùng seed trận do host cung cấp. Source nội bộ
chưa có LICENSE chung; shader/package Unity theo điều khoản nhà cung cấp.
Bằng chứng Native và phần hiển thị/LAN chưa kiểm chứng được ghi trong docs/testing.md.

## Bổ sung SB-46 — dữ liệu navigation, 22/11/2026

Hazard hiện có chỉ bổ sung hint sàn/cảnh báo cho AI qua Core, với hỗ trợ AI Codex
(C#, Unity 6000.3.28f1, GDD A.3/A.4); không thêm ảnh/model/font/âm thanh hoặc vật liệu.
Hình học H01–H07 giữ nguyên; source H06 có hash mới trong CSV vì thêm API chỉ đọc,
hash/commit trước đó được ghi ở Phụ lục C. Bot dùng seed trận/ID cho RNG; không có
prompt image generation. Nguồn và giới hạn giấy phép của asset tái dùng không đổi.


## SB-17 — nguồn HUD v2 và bằng chứng review, 23/11/2026

Giữ hình vòng cooldown và badge procedural của v1; v2 sửa bố cục Wobble trong
Assets/_Project/Scripts/UI/HUD/MatchHud.cs với hỗ trợ AI Codex, theo issue #15/GDD11.2.
Không dùng model sinh ảnh, prompt ảnh hoặc RNG. Tám PNG trong
docs/testing/sb17-hud-review là ScreenCapture Windows Player Direct3D12 nguyên bản,
không phải ảnh AI hay asset runtime mới. Hash nguồn và bằng chứng trong manifest.json.
Nguồn nhóm tạo chưa có LICENSE chung; font/uGUI theo điều khoản Unity hiện hữu.
