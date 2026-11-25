# Appendix C. Asset and AI-generation ledger

> **In this chapter:** Every asset with its generator, licence and AI declaration.

## C.1 Current asset ledger

### Hồ sơ runtime A02 (kiểm kê 20/11/2026)

Danh mục từng tệp hiện có, SHA-256, nguồn, công cụ, seed và vị trí sử dụng nằm tại
[`../asset-ledger.csv`](../asset-ledger.csv); credits và giới hạn xác minh nằm tại
[`../../CREDITS.md`](../../CREDITS.md). Mốc kiểm kê: main `b856485`.
Danh sách A01–A15 dưới đây là hồ sơ bộ minh họa GDD; không đồng nghĩa mọi tệp đã nhập vào game.
Runtime hiện có sáu model procedural từ `tools/models/make_fruits.py` và 12 SFX từ
`tools/audio/make_sfx.py`; font đang dùng là LegacyRuntime của Unity, chưa nhập Baloo 2/Fredoka,
chưa có nhạc. Prompt gốc chưa có trong checkout được ghi là thiếu, không dựng lại như dữ liệu gốc.
Thay đổi này bổ sung hồ sơ nguồn, không thay thiết kế hay ghi nhận tính năng đã nghiệm thu.

This ledger lists only the active assets used in version 1.4 (Creative's 3D/UX-UI deliverables, technical diagrams and placeholder SFX). All superseded old-concept fruit art (`A01–A05`, `A07–A10`, `A12–A13`, `A15`, rejected FLUX images) has been removed.

| ID | Asset | Generator | Licence and declaration |
|---|---|---|---|
| SB76-CH | Six 3D animal fighters: `Bear.glb`, `Cat.glb`, `Chick.glb`, `Frog.glb`, `Penguin.glb`, `Pig.glb` (`Assets/_Project/Art/Characters/`) | Blender / glTF (Creative team, SB-76) | Internal team asset (approved); fully replaces earlier concept |
| SB76-AV | Six UI fighter avatars: `avatar_0.png` … `avatar_5.png` (`Assets/_Project/Art/UI/Resources/UI/`) | Rendered from SB-76 3D character models | Internal team asset (approved); used in HUD, Lobby and Results |
| SB76-LOB | STAR CATCHER 3D claw-machine lobby: `Assets/_Project/Art/Lobby/Lobby.glb` | Blender / glTF (Creative team, SB-76) | Internal team asset (approved); no Sketchfab sources |
| SB75-UI | Core UX/UI kit (Logo, Start Screen `bg_start.png`, Settings, Pause, Round Score, WINNER Podium, HUD hearts/timer/board/weight, medals) | Creative team UX/UI kit (SB-75) | Internal team asset (approved); palette `#410E9E`, `#FD5FB0`, `#3ED5D0` |
| SB77-UI | Lobby UI kit: `console_lobby.png`, `console_rooms.png`, `lobby_row.png`, `card_heavy.png`, `card_normal.png`, `card_light.png` (`Assets/_Project/Art/UI/Resources/UI/`) | Creative team UX/UI kit (SB-77) | Internal team asset (approved); used for Lobby Console, Find Server and class stat cards |
| SB78-L1 | L1 Training Ring: `Map1Demo.glb`; round floats `PhaoTron1.glb`, `PhaoTron2.glb`, `PhaoTron3.glb`; unicorn floats `PhaoUnicorn1.glb`, `PhaoUnicorn2.glb`, `PhaoUnicorn3.glb`; four island rings `Vong1.glb`, `Vong2.glb`, `Vong3.glb`, `Vong4.glb` (`Assets/_Project/Art/Levels/L1/`) | Blender / glTF (Creative team, SB-78) | Internal team asset (approved); fighter visual scale `0.95×` |
| SB78-L2 | L2 Moving Ground: `Assets/_Project/Art/Levels/L2/L2.glb` | Blender / glTF (Creative team, SB-78) | Internal team asset (approved); fighter visual scale `0.80×` |
| SB78-L3 | L3 Cannon Court: `Assets/_Project/Art/Levels/L3/Map3.glb` | Blender / glTF (Creative team, SB-78) | Internal team asset (approved); fighter visual scale `0.60×` |
| SB78-L4 | L4 Frost Ring: `Assets/_Project/Art/Levels/L4/map 4.glb` and ice tile `cuc bang 2.glb` | Blender / glTF (Creative team, SB-78) | Internal team asset (approved); fighter visual scale `0.90×` |
| SB78-L5 | L5 Chaos Core: `Assets/_Project/Art/Levels/L5/L5.glb` | Blender / glTF (Creative team, SB-78) | Internal team asset (approved); fighter visual scale `0.85×` |
| A06 | Top-down physics layouts ×5 (L1–L5) | SVG (Python) | Procedural; dimensions from Chapters 6–7 |
| A11 | Screen flow diagram | SVG (Python) | Procedural |
| A14 | SFX placeholders ×12 (`Assets/_Project/Audio/SFX/*.wav`) | numpy (44.1 kHz mono WAV) | Procedural; wired in Unity |
| F1–F4 | Core loop, game flow, difficulty curve and architecture figures | Mermaid CLI and matplotlib | Procedural |
| FONT | Fonts: Sleeping (Creative Drive) and Baloo 2 / Fredoka | Creative UX/UI pack & Google Fonts | Sleeping (Creative pack) · Baloo 2 / Fredoka (SIL OFL 1.1) |

All 3D characters, the STAR CATCHER lobby, all five L1–L5 arenas and the UX/UI kit are authored directly by the Creative team (SB-74–SB-78); procedural scripts are used only for technical GDD diagrams and the 12 placeholder sound effects.

Text of this GDD was drafted with the help of an AI assistant (Claude) and reviewed by the team; every member can explain the design decisions in their own chapters.

## C.2 Asset groups

### HUD procedural của SB-17 (bản kiểm thử 21/11/2026)

`UI/HUD/MatchHud.cs` tạo vòng cooldown 64 × 64 pixel bằng C#/Texture2D lúc chạy,
uGUI Filled Radial360; không có tệp ảnh nhập ngoài. Panel/chữ dùng `UiFactory` và
font LegacyRuntime tích hợp Unity. Công cụ: Unity 6000.3.28f1, code có Codex hỗ trợ;
yêu cầu nguồn: HUD GDD 11.2 (đồng hồ, số quả, dash, hạng cân và Wobble). Không dùng
seed hoặc model sinh ảnh; không có prompt gốc ngoài yêu cầu issue #15 và mã trong Git.
Sử dụng trong trận qua `App/MatchHost`; phiên bản SB-17 v1. Phần code/ảnh procedural do
dự án tạo; repo chưa có LICENSE chung, thành phần uGUI/font theo điều khoản Unity.
Native EditMode 165/165 và PlayMode 38/38 đã đạt; xem bằng chứng build/bố cục trong
`docs/testing.md`. Đây là bản kiểm thử, chưa ghi asset đã được nhóm duyệt.

| ID | Nội dung runtime | Nguồn và vị trí dùng |
|---|---|---|
| ICO-HUD-DASH01 | Texture và Sprite vòng cooldown 64 × 64 | `Assets/_Project/Scripts/UI/HUD/MatchHud.cs`; HUD dưới trái |
| ICO-HUD-CLASS01 | Huy hiệu chữ hạng cân và kg, nền uGUI tương phản | Cùng mã nguồn; HUD dưới trái, dữ liệu từ `FruitDefinition.Class` |
### Cập nhật UI runtime SB-24 (21/11/2026)

Bản ghi `RT-UI` trong `docs/asset-ledger.csv` bổ sung kết quả trận được tạo bằng
`UI/Menus/MatchOverlay.cs`. Công cụ Unity 6000.3.28f1, C#/uGUI và font LegacyRuntime
tích hợp; Codex hỗ trợ sửa code. Không thêm ảnh, font, model bên ngoài, không dùng
seed/model sinh ảnh; nguồn yêu cầu là issue #22 và GDD 10.4/11.3. Nội dung do nhóm
tạo chưa có LICENSE chung, thành phần Unity theo điều khoản nhà cung cấp. Native
loopback xác nhận nội dung chữ hai peer khớp; bố cục chưa được nhóm duyệt.

### Asset greybox L1 của SB-14

Các tệp dưới đây thuộc phiên bản greybox SB-14 v1, tạo bằng C# và Unity
6000.3.28f1 với hỗ trợ của Codex. Mesh được tạo từ công thức vòng tròn 120 phân
đoạn và màu pastel đặt trực tiếp trong Unity. Yêu cầu nguồn là dựng L1 theo
GDD 9.2.1, với 4 dải rộng 3 m và placeholder quả hạng Vừa. Không dùng seed,
mô hình tạo ảnh, texture tải ngoài hoặc gói asset bên thứ ba. Mesh và giá trị
màu do dự án tạo. Shader URP và primitive Sphere lấy từ Unity, dùng theo giấy
phép của Unity và gói URP đi kèm dự án.

| ID | Tệp trong `Assets/_Project/Data/TrainingRing/` | Vị trí sử dụng trong `L1.unity` |
|---|---|---|
| ENV-L1-GB01 | `Band1.asset` | Mesh mặt sàn và collider dải 0–3 m |
| ENV-L1-GB02 | `Band2.asset` | Mesh mặt sàn và collider dải 3–6 m |
| ENV-L1-GB03 | `Band3.asset` | Mesh mặt sàn và collider dải 6–9 m |
| ENV-L1-GB04 | `Band4.asset` | Mesh mặt sàn và collider dải 9–12 m |
| ENV-L1-GB05 | `Band1.mat` | Màu dải 0–3 m |
| ENV-L1-GB06 | `Band2.mat` | Màu dải 3–6 m |
| ENV-L1-GB07 | `Band3.mat` | Màu dải 6–9 m |
| ENV-L1-GB08 | `Band4.mat` | Màu dải 9–12 m |
| CH-L1-GB01 | `FruitPlaceholder.mat` | Màu của Sphere tích hợp sẵn tại sáu spawn. Các quả xem trước mặc định tắt. |

The group codes below organise content; each file gets its own ID to track versions and provenance. The list defines what must be managed and does not claim that every asset is finished.

| Code | Group | Content | Used in |
|---|---|---|---|
| CH | Characters | Six jelly fruits in three sizes, shared face set, expressions | Lobby, matches and results |
| AN | Animation | Roll, jelly squash, jump, land, dash, impact, fall, respawn, celebrate | Fruits and cutscenes |
| ENV | Environment | Five toy-store play areas, lobby aisle with shelves, fruit stands, host desk and store decoration | L1–L5, lobby, opening and ending |
| HZ | Hazards | Ring bands, turntable, obstacle ring and boxes, pusher blocks, air and fire cannons, ice tiles | Matching arenas |
| UI | Interface | Menus, Host/Join, rule select, 3D lobby panels, HUD and results | Interface screens |
| ICO | Icons | Three class badges, lives, respawn, dash cooldown, fruits left and hazard warnings | HUD, tutorial and level cards |
| FX | Effects | Dash, impact, ring drops, cannons, warnings, breaking floor, falls and respawn | Action and hazard feedback |
| SFX | Sound effects | Rolling, squish impact, dash, cannons, warnings, breaking floor, falls, respawn, win/lose and UI | Gameplay and interface |
| MUS | Music | Menu, match and result music | Matching states |
| NAR | Story content | Opening and ending images or animation and play-area cards | Appendix B |
| DOC | GDD illustrations | Maps, diagrams, mockups and reference images | Design chapters |

## C.3 Record fields

| Field | Content |
|---|---|
| Asset ID | Unique code, for example CH-001 or SFX-001 |
| Name and type | Model, texture, animation, sound, illustration or other |
| File and location | File name, format and path in the project |
| Version | Current version and update date |
| Origin | Creator or original source; external components if any |
| Generation method | Tool, model, prompt and seed; AI declaration |
| Team edits | Geometry, colour, material, layout or volume changes |
| Usage information | Licence, source link and attribution |
| Used in | Scene, prefab, screen or GDD chapter |
| Status | Planned, in progress, placeholder, approved or retired |

## C.4 Status conventions

| Status | Meaning |
|---|---|
| Planned | Need identified, no file yet |
| In progress | Being created or edited |
| Placeholder | Used for testing, to be replaced |
| Approved | Accepted by the team for the recorded use |
| Retired | Replaced or removed from the current version |

When an asset is replaced, its version, path and usage are updated and the old record is kept for comparison. Changes that affect the design or production scope are also logged in Appendix D.

## C.5 H06 procedural — SB-44, 21/11/2026

| Trường | Ghi nhận |
|---|---|
| Asset ID | H06-PROC-001 |
| Loại, phiên bản | Mesh hex prism, màu băng và vết nứt LineRenderer runtime; source v2, đã kiểm tra Native hành vi ô; hiển thị chưa nghiệm thu |
| Nguồn/vị trí | Nhóm 18; `Assets/_Project/Scripts/Hazards/IceTiles.cs`, cấu hình `Core/IceProfile.cs` |
| Công cụ/phương pháp | C# Unity Mesh API và LineRenderer, có hỗ trợ AI Codex; không dùng ảnh/texture/mô hình tải ngoài |
| Prompt/seed | Yêu cầu triển khai issue SB-44 theo GDD 7.4, 9.2.4; không có prompt image generation. Hình học không ngẫu nhiên; chọn ô dùng seed trận do host truyền |
| Sử dụng | Scene L4 qua `Editor/IceSceneBuilder.cs`; scene, IceProfile.asset và meta đã được Editor Native sinh |
| Giấy phép/attribution | Source nội bộ trong repo; shader URP từ package Unity hiện hữu. Không suy diễn giấy phép riêng hoặc nguồn bên ngoài |
| Trạng thái | Native IceArenaTests 3/3; danh mục nguồn ở CREDITS.md và docs/asset-ledger.csv. Chưa quan sát hình ảnh player hoặc LAN nhiều máy |

Lịch sử H06-PROC-001: source v2 tại commit `85b3c9c`, SHA-256 `a9325e57f3b192414997d3bd1389f57ba17ae93082b54610be1e3ad3455391a2`;
source v3 sửa replay qua nhiều chu kỳ, SHA mới trong CSV. Giữ đường mesh/material
và thông số; không bổ sung asset bên ngoài hoặc thay thiết kế. Bản v2 vẫn truy vết qua commit trên.

## C.6 L5 tái dùng asset — SB-45, 21/11/2026

ENV-L5-001 là scene L5 do Editor Native tạo bằng IceSceneBuilder.BuildL5 với hỗ trợ
AI Codex; danh mục CSV ghi hash scene. Tái dùng H06-PROC-001/H06-CONFIG-001 cho cả
L4 và L5, Cannons/HazardProfile từ main cho H04/H05. Không sinh bitmap/model mới;
không có prompt image generation hoặc seed của hình học. Seed chọn ô do host cấp.
Vị trí, nguồn công cụ và giới hạn giấy phép theo CREDITS.md. Native tests không
chứng minh chất lượng hiển thị, khả năng đọc cảnh báo hoặc LAN nhiều máy.

## C.7 SB-46 — hint navigation, 22/11/2026

Các component hazard chỉ thêm API cấp dữ liệu bot, không thêm hình ảnh/model/vật liệu.
H06-PROC-001 tại c918aa6 có hash `17f59ffce25a69f7b385b94c677bcf2128507386fc6293c80feff122da4a59e9`;
phiên bản có hint mới lưu hash trong CSV. Giữ c918aa6 để truy vết source v3 trước
thay đổi. Công cụ C#/Unity 6000.3.28f1, có hỗ trợ AI Codex theo issue SB-46 và GDD
A.3/A.4; không có prompt image generation hoặc asset tải ngoài. RNG bot dùng seed
trận/ID; hình học không đổi. Nguồn/giấy phép giữ theo CREDITS.md, không suy diễn
giấy phép chung của repo. Native test hành vi không thay thế kiểm tra hình ảnh.


## SB-17 — nguồn HUD v2 và bằng chứng review, 23/11/2026

Giữ hình vòng cooldown và badge procedural của v1; v2 sửa bố cục Wobble trong
Assets/_Project/Scripts/UI/HUD/MatchHud.cs với hỗ trợ AI Codex, theo issue #15/GDD11.2.
Không dùng model sinh ảnh, prompt ảnh hoặc RNG. Tám PNG trong
docs/testing/sb17-hud-review là ScreenCapture Windows Player Direct3D12 nguyên bản,
không phải ảnh AI hay asset runtime mới. Hash nguồn và bằng chứng trong manifest.json.
Nguồn nhóm tạo chưa có LICENSE chung; font/uGUI theo điều khoản Unity hiện hữu.
## Lobby runtime — SB-25, bản kiểm thử 21/11/2026

| ID | Nguồn/phương pháp | Vị trí và trạng thái |
|---|---|---|
| LOB-PROC-002 | Nhóm 18, C# có hỗ trợ AI Codex, Unity primitive sphere/material URP; avatar trung tính chưa chọn trái | `App/LobbyController.cs`; kiểm thử Native theo `docs/testing.md` |
| LOB-UI-002 | Nhóm 18, C# có hỗ trợ AI Codex, uGUI/LegacyRuntime.ttf builtin; info/controls, nhãn khóa kệ world-space và desk/ready overlay | `UI/LobbyPanel.cs`; Native kiểm tra nội dung/trạng thái, chưa duyệt glyph/đọc được từ camera |
| LOB-CONFIG-001 | `LobbyProfile.asset` được Editor API tạo/gán walking class, spawn, desk reach và countdown | `Data/LobbyProfile.asset`; hash/nguồn trong CSV |

Yêu cầu: nối runtime lobby 3D theo issue #23 và GDD 8.5/11.1 trên greybox PR #95.
Không có ảnh AI, prompt imagegen hay seed hình học. Display và avatar đã chọn tái
sử dụng sáu prefab/GLB hiện hữu, không tạo bản model tải ngoài. Shader/font builtin
theo package Unity hiện có; không suy diễn giấy phép ngoài repo. Ledger tổng hợp
`docs/asset-ledger.csv` đã bổ sung ba bản ghi này, phiên bản SB-25 v1.1 ngày 21/11/2026.
Mã nhóm tạo chưa có LICENSE chung; thành phần Unity theo điều khoản nhà cung cấp.
Không có bản prompt gốc riêng ngoài yêu cầu issue và mã trong Git. Chưa ghi approved.


SB-26 (24/11/2026): CFG-SINGLE-001 là MatchSetup.asset được Unity 6000.3.28f1
sửa qua SinglePlayerSetup.Configure để tham chiếu D1–D5 hiện hữu. Codex hỗ trợ
code luồng/roster và helper Editor theo GDD 3.5/A.6; không thêm model/ảnh/font/SFX
hay asset sinh ảnh. D39 giữ các slot/hạng cân khi đổi sang thú. Không dùng RNG
để sinh asset; RNG gameplay do host giữ seed. Hash config trong asset-ledger.csv;
nguồn/giấy phép tài nguyên UI/nhân vật tái dùng giữ nguyên.
